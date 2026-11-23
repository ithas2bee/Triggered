import { fireEvent, render, waitFor } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';

import type { Food } from '../src/application/foods/Food';
import type { FoodRepository } from '../src/application/foods/FoodRepository';
import { FoodListScreen } from '../src/presentation/screens/FoodListScreen';

const food: Food = { id: 'food-id', name: 'Ketchup' };

function createRepository(
  overrides: Partial<{
    [Method in keyof FoodRepository]: jest.MockedFunction<
      FoodRepository[Method]
    >;
  }> = {},
): jest.Mocked<FoodRepository> {
  const repository: jest.Mocked<FoodRepository> = {
    getFoods: jest.fn().mockResolvedValue([]),
    createFood: jest.fn().mockResolvedValue(food),
  };

  if (overrides.getFoods) {
    repository.getFoods = overrides.getFoods;
  }
  if (overrides.createFood) {
    repository.createFood = overrides.createFood;
  }

  return repository;
}

function renderFoodListScreen(foodRepository: jest.Mocked<FoodRepository>) {
  return render(
    <NavigationContainer>
      <FoodListScreen foodRepository={foodRepository} />
    </NavigationContainer>,
  );
}

describe('FoodListScreen', () => {
  it('shows a loading state while Foods are requested', async () => {
    let resolveFoods!: (foods: Food[]) => void;
    const foodRepository = createRepository({
      getFoods: jest.fn(
        () =>
          new Promise<Food[]>((resolve) => {
            resolveFoods = resolve;
          }),
      ),
    });
    const screen = await renderFoodListScreen(foodRepository);

    expect(screen.getByLabelText('Loading foods')).toBeTruthy();

    resolveFoods([]);
    await waitFor(() => {
      expect(screen.getByText('No foods yet. Add a food to get started.')).toBeTruthy();
    });
  });

  it('displays saved Foods and the empty state when the collection is empty', async () => {
    const existingFoods = [
      food,
      { id: 'food-id-2', name: 'Tomato' },
    ];
    const foodRepository = createRepository({
      getFoods: jest.fn().mockResolvedValue(existingFoods),
    });
    const screen = await renderFoodListScreen(foodRepository);

    expect(await screen.findByText('Ketchup')).toBeTruthy();
    expect(screen.getByText('Tomato')).toBeTruthy();

    const emptyScreen = await renderFoodListScreen(createRepository());
    expect(
      await emptyScreen.findByText('No foods yet. Add a food to get started.'),
    ).toBeTruthy();
    expect(emptyScreen.getByRole('button', { name: 'Add a food' })).toBeTruthy();
  });

  it('creates a Food and updates the list', async () => {
    const foodRepository = createRepository();
    const screen = await renderFoodListScreen(foodRepository);

    await screen.findByText('No foods yet. Add a food to get started.');
    await fireEvent.press(screen.getByRole('button', { name: 'Add a food' }));
    await fireEvent.changeText(screen.getByLabelText('Food name'), 'Ketchup');
    await fireEvent.press(screen.getByRole('button', { name: 'Save food' }));

    expect(await screen.findByText('Ketchup')).toBeTruthy();
    expect(foodRepository.createFood).toHaveBeenCalledWith('Ketchup');
    expect(screen.queryByLabelText('Food name')).toBeNull();
  });

  it('blocks blank names and preserves input when the API rejects creation', async () => {
    const foodRepository = createRepository({
      createFood: jest.fn().mockRejectedValue(new Error('Name is required.')),
    });
    const screen = await renderFoodListScreen(foodRepository);

    await screen.findByText('No foods yet. Add a food to get started.');
    await fireEvent.press(screen.getByRole('button', { name: 'Add a food' }));
    const input = screen.getByLabelText('Food name');
    await fireEvent.changeText(input, '   ');
    expect(screen.getByRole('button', { name: 'Save food' })).toBeDisabled();
    expect(foodRepository.createFood).not.toHaveBeenCalled();

    await fireEvent.changeText(input, ' Ketchup ');
    await fireEvent.press(screen.getByRole('button', { name: 'Save food' }));

    expect(await screen.findByText('Name is required.')).toBeTruthy();
    expect(screen.getByLabelText('Food name').props.value).toBe(' Ketchup ');
  });

  it('offers retry after a list request fails', async () => {
    const foodRepository = createRepository({
      getFoods: jest
        .fn()
        .mockRejectedValueOnce(new Error('Could not connect.'))
        .mockResolvedValueOnce([food]),
    });
    const screen = await renderFoodListScreen(foodRepository);

    expect(await screen.findByText('Could not connect.')).toBeTruthy();
    await fireEvent.press(screen.getByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('Ketchup')).toBeTruthy();
    expect(foodRepository.getFoods).toHaveBeenCalledTimes(2);
  });

  it('prevents duplicate submissions while creation is pending', async () => {
    let resolveCreation!: (createdFood: Food) => void;
    const foodRepository = createRepository({
      createFood: jest.fn(
        (_name: string) =>
          new Promise<Food>((resolve) => {
            resolveCreation = resolve;
          }),
      ),
    });
    const screen = await renderFoodListScreen(foodRepository);

    await screen.findByText('No foods yet. Add a food to get started.');
    await fireEvent.press(screen.getByRole('button', { name: 'Add a food' }));
    await fireEvent.changeText(screen.getByLabelText('Food name'), 'Ketchup');
    await fireEvent.press(screen.getByRole('button', { name: 'Save food' }));
    await fireEvent.press(screen.getByRole('button', { name: 'Saving…' }));

    expect(foodRepository.createFood).toHaveBeenCalledTimes(1);

    resolveCreation(food);
    expect(await screen.findByText('Ketchup')).toBeTruthy();
  });
});
