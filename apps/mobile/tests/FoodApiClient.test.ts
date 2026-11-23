import { FoodApiClient } from '../src/infrastructure/api/FoodApiClient';

function jsonResponse(body: unknown, status = 200): Response {
  return {
    json: jest.fn().mockResolvedValue(body),
    ok: status >= 200 && status < 300,
    status,
  } as unknown as Response;
}

describe('FoodApiClient', () => {
  beforeEach(() => {
    jest.restoreAllMocks();
  });

  it('gets and parses the Food collection', async () => {
    const foods = [{ id: 'food-id', name: 'Ketchup' }];
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse(foods));
    const client = new FoodApiClient('http://api.example.test/');

    await expect(client.getFoods()).resolves.toEqual(foods);

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.example.test/api/foods',
      expect.objectContaining({
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
      }),
    );
  });

  it('posts the Food name and parses the created Food', async () => {
    const food = { id: 'food-id', name: 'Ketchup' };
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse(food, 201));
    const client = new FoodApiClient('http://api.example.test');

    await expect(client.createFood('Ketchup')).resolves.toEqual(food);

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.example.test/api/foods',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'Ketchup' }),
      }),
    );
  });

  it('uses the API validation detail for a rejected Food name', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ errors: { Name: ['Name is required.'] } }, 400),
      );
    const client = new FoodApiClient('http://api.example.test');

    await expect(client.createFood(' ')).rejects.toThrow('Name is required.');
  });

  it('reports server and network errors without leaking response details', async () => {
    jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({}, 500));
    const client = new FoodApiClient('http://api.example.test');

    await expect(client.createFood('Ketchup')).rejects.toThrow(
      'The server could not save the food. Please try again.',
    );

    jest
      .spyOn(globalThis, 'fetch')
      .mockRejectedValueOnce(new Error('connection refused'));

    await expect(client.getFoods()).rejects.toThrow(
      'Could not connect to the server. Check your connection and try again.',
    );
  });
});
