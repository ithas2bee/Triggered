import { fireEvent, render, waitFor } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';

import type { Food } from '../src/application/foods/Food';
import type { FoodRepository } from '../src/application/foods/FoodRepository';
import type { CreateObservationRequest } from '../src/application/observations/CreateObservationRequest';
import type { Observation } from '../src/application/observations/Observation';
import type { ObservationRepository } from '../src/application/observations/ObservationRepository';
import { ObservationEntryScreen } from '../src/presentation/screens/ObservationEntryScreen';

const foods: Food[] = [
  { id: 'food-guid-1', name: 'Ketchup' },
  { id: 'food-guid-2', name: 'Tomato' },
];

function createRepositories(
  overrides: {
    getFoods?: jest.MockedFunction<FoodRepository['getFoods']>;
    createObservation?: jest.MockedFunction<
      ObservationRepository['createObservation']
    >;
  } = {},
) {
  const foodRepository: jest.Mocked<FoodRepository> = {
    getFoods: overrides.getFoods ?? jest.fn().mockResolvedValue(foods),
    createFood: jest.fn(),
  };
  const observationRepository: jest.Mocked<ObservationRepository> = {
    createObservation:
      overrides.createObservation ??
      jest.fn((request: CreateObservationRequest) =>
        Promise.resolve({ id: 'observation-id', ...request }),
      ),
  };

  return { foodRepository, observationRepository };
}

function renderScreen({
  foodRepository,
  observationRepository,
}: ReturnType<typeof createRepositories>) {
  return render(
    <NavigationContainer>
      <ObservationEntryScreen
        foodRepository={foodRepository}
        observationRepository={observationRepository}
      />
    </NavigationContainer>,
  );
}

describe('ObservationEntryScreen', () => {
  it('loads saved Foods and lets the user select the Food to record', async () => {
    const { foodRepository, observationRepository } = createRepositories();
    const screen = await renderScreen({ foodRepository, observationRepository });

    expect(await screen.findByRole('radio', { name: 'Ketchup' })).toBeTruthy();
    await fireEvent.press(screen.getByRole('radio', { name: 'Tomato' }));
    await fireEvent.changeText(screen.getByLabelText('Reaction'), 'Heartburn');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );

    await waitFor(() => {
      expect(observationRepository.createObservation).toHaveBeenCalledWith(
        expect.objectContaining({ foodId: 'food-guid-2' }),
      );
    });
    expect(foodRepository.getFoods).toHaveBeenCalledTimes(1);
  });

  it('requires an explicit Food selection', async () => {
    const { foodRepository, observationRepository } = createRepositories();
    const screen = await renderScreen({ foodRepository, observationRepository });

    await screen.findByRole('radio', { name: 'Ketchup' });
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );

    expect(await screen.findByText('Select a saved food.')).toBeTruthy();
    expect(observationRepository.createObservation).not.toHaveBeenCalled();
  });

  it('blocks empty reaction and invalid date/time before submission', async () => {
    const { foodRepository, observationRepository } = createRepositories();
    const screen = await renderScreen({ foodRepository, observationRepository });

    await screen.findByRole('radio', { name: 'Ketchup' });
    await fireEvent.press(screen.getByRole('radio', { name: 'Ketchup' }));
    await fireEvent.changeText(screen.getByLabelText('Consumption date'), '2026-02-30');
    await fireEvent.changeText(screen.getByLabelText('Consumption time'), '25:30');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );

    expect(
      await screen.findByText('Enter a valid date and time.'),
    ).toBeTruthy();
    expect(screen.getByText('Enter the reaction you experienced.')).toBeTruthy();
    expect(observationRepository.createObservation).not.toHaveBeenCalled();
  });

  it('submits the selected Food and an explicit-offset timestamp, then confirms success', async () => {
    const { foodRepository, observationRepository } = createRepositories();
    const screen = await renderScreen({ foodRepository, observationRepository });

    await screen.findByRole('radio', { name: 'Ketchup' });
    await fireEvent.press(screen.getByRole('radio', { name: 'Ketchup' }));
    await fireEvent.changeText(screen.getByLabelText('Consumption date'), '2026-10-09');
    await fireEvent.changeText(screen.getByLabelText('Consumption time'), '11:30');
    await fireEvent.changeText(screen.getByLabelText('Reaction'), 'Heartburn');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );

    await waitFor(() => {
      expect(observationRepository.createObservation).toHaveBeenCalledWith({
        foodId: 'food-guid-1',
        consumedAt: new Date(2026, 9, 9, 11, 30).toISOString(),
        reaction: 'Heartburn',
      });
    });
    expect(
      await screen.findByRole('header', { name: 'Observation recorded' }),
    ).toBeTruthy();
    expect(screen.getByText('Recorded Heartburn for Ketchup.')).toBeTruthy();
  });

  it('prevents duplicate submissions while pending', async () => {
    let resolveCreation!: (observation: {
      id: string;
      foodId: string;
      consumedAt: string;
      reaction: string;
    }) => void;
    const { foodRepository, observationRepository } = createRepositories({
      createObservation: jest.fn(
        (_request: CreateObservationRequest) =>
          new Promise<Observation>((resolve) => {
            resolveCreation = resolve;
          }),
      ),
    });
    const screen = await renderScreen({ foodRepository, observationRepository });

    await screen.findByRole('radio', { name: 'Ketchup' });
    await fireEvent.press(screen.getByRole('radio', { name: 'Ketchup' }));
    await fireEvent.changeText(screen.getByLabelText('Reaction'), 'Heartburn');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );
    const pendingButton = screen.getByRole('button', {
      name: 'Saving observation…',
    });
    expect(pendingButton).toBeDisabled();
    await fireEvent.press(pendingButton);
    expect(observationRepository.createObservation).toHaveBeenCalledTimes(1);

    resolveCreation({
      id: 'observation-id',
      foodId: foods[0].id,
      consumedAt: '2026-10-09T11:30:00Z',
      reaction: 'Heartburn',
    });
    expect(
      await screen.findByRole('header', { name: 'Observation recorded' }),
    ).toBeTruthy();
  });

  it('preserves form values after an API error so the user can retry', async () => {
    const createObservation = jest
      .fn()
      .mockRejectedValueOnce(new Error('The server could not save the observation. Please try again.'))
      .mockResolvedValueOnce({
        id: 'observation-id',
        foodId: foods[0].id,
        consumedAt: '2026-10-09T11:30:00Z',
        reaction: 'Heartburn',
      });
    const { foodRepository, observationRepository } = createRepositories({
      createObservation,
    });
    const screen = await renderScreen({ foodRepository, observationRepository });

    await screen.findByRole('radio', { name: 'Ketchup' });
    await fireEvent.press(screen.getByRole('radio', { name: 'Ketchup' }));
    await fireEvent.changeText(screen.getByLabelText('Consumption date'), '2026-10-09');
    await fireEvent.changeText(screen.getByLabelText('Consumption time'), '11:30');
    await fireEvent.changeText(screen.getByLabelText('Reaction'), 'Heartburn');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );

    expect(
      await screen.findByText(
        'The server could not save the observation. Please try again.',
      ),
    ).toBeTruthy();
    expect(screen.getByLabelText('Consumption date').props.value).toBe('2026-10-09');
    expect(screen.getByLabelText('Consumption time').props.value).toBe('11:30');
    expect(screen.getByLabelText('Reaction').props.value).toBe('Heartburn');
    expect(
      screen.getByRole('radio', { name: 'Ketchup' }).props.accessibilityState,
    ).toMatchObject({ checked: true });

    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );
    expect(
      await screen.findByRole('header', { name: 'Observation recorded' }),
    ).toBeTruthy();
  });
});
