import { ObservationApiClient } from '../src/infrastructure/api/ObservationApiClient';

function jsonResponse(body: unknown, status = 200): Response {
  return {
    json: jest.fn().mockResolvedValue(body),
    ok: status >= 200 && status < 300,
    status,
  } as unknown as Response;
}

describe('ObservationApiClient', () => {
  beforeEach(() => {
    jest.restoreAllMocks();
  });

  it('posts the documented request and parses the created Observation', async () => {
    const request = {
      foodId: '65fbe76e-aea0-4c42-a9ab-6a27e507d215',
      consumedAt: '2026-10-09T11:30:00.000Z',
      reaction: 'Heartburn',
    };
    const observation = {
      id: '8379c0a1-810e-44a8-8fc6-6f4e2b7770c1',
      ...request,
    };
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse(observation, 201));
    const client = new ObservationApiClient('http://api.example.test/');

    await expect(client.createObservation(request)).resolves.toEqual(
      observation,
    );

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.example.test/api/observations',
      {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      },
    );
  });

  it('uses the API validation detail for a rejected request', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ errors: { Reaction: ['Reaction is required.'] } }, 400),
      );
    const client = new ObservationApiClient('http://api.example.test');

    await expect(
      client.createObservation({
        foodId: 'food-id',
        consumedAt: '2026-10-09T11:30:00Z',
        reaction: ' ',
      }),
    ).rejects.toThrow('Reaction is required.');
  });

  it('reports not-found and server responses without discarding request data', async () => {
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse({}, 404))
      .mockResolvedValueOnce(jsonResponse({}, 500));
    const client = new ObservationApiClient('http://api.example.test');
    const request = {
      foodId: 'food-id',
      consumedAt: '2026-10-09T11:30:00Z',
      reaction: 'Heartburn',
    };

    await expect(client.createObservation(request)).rejects.toThrow(
      'That food could not be found. Choose a saved food and try again.',
    );
    await expect(client.createObservation(request)).rejects.toThrow(
      'The server could not save the observation. Please try again.',
    );
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      'http://api.example.test/api/observations',
      expect.objectContaining({ body: JSON.stringify(request) }),
    );
  });

  it('reports network failures and missing API configuration', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockRejectedValue(new Error('connection refused'));
    const client = new ObservationApiClient('http://api.example.test');
    const request = {
      foodId: 'food-id',
      consumedAt: '2026-10-09T11:30:00Z',
      reaction: 'Heartburn',
    };

    await expect(client.createObservation(request)).rejects.toThrow(
      'Could not connect to the server. Check your connection and try again.',
    );
    await expect(
      new ObservationApiClient().createObservation(request),
    ).rejects.toThrow(
      'Set EXPO_PUBLIC_API_BASE_URL to connect to the Triggered API.',
    );
  });
});
