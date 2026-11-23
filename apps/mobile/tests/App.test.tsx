import { fireEvent, render } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';

import type { FoodRepository } from '../src/application/foods/FoodRepository';
import type { ObservationRepository } from '../src/application/observations/ObservationRepository';
import { RootNavigator } from '../src/presentation/navigation/RootNavigator';
import App from '../src/presentation/App';

describe('mobile app', () => {
  it('renders the Triggered home screen at startup', async () => {
    const { getByRole, getByText } = await render(<App />);

    expect(getByRole('header', { name: 'Triggered' })).toBeTruthy();
    expect(getByText('Your personal food and reaction journal')).toBeTruthy();
  });

  it('navigates from home to the Food list', async () => {
    const { getByRole } = await render(<App />);

    await fireEvent.press(getByRole('button', { name: 'Food list' }));

    expect(getByRole('header', { name: 'Foods' })).toBeTruthy();
  });

  it('starts Observation logging from the existing Food list', async () => {
    const foodRepository: jest.Mocked<FoodRepository> = {
      getFoods: jest.fn().mockResolvedValue([
        { id: 'food-guid', name: 'Ketchup' },
      ]),
      createFood: jest.fn(),
    };
    const observationRepository: jest.Mocked<ObservationRepository> = {
      createObservation: jest.fn().mockResolvedValue({
        id: 'observation-id',
        foodId: 'food-guid',
        consumedAt: '2026-10-09T11:30:00Z',
        reaction: 'Heartburn',
      }),
    };
    const screen = await render(
      <NavigationContainer>
        <RootNavigator
          foodRepository={foodRepository}
          observationRepository={observationRepository}
        />
      </NavigationContainer>,
    );

    await fireEvent.press(screen.getByRole('button', { name: 'Food list' }));
    await screen.findByText('Ketchup');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Log an observation' }),
    );

    expect(
      await screen.findByRole('header', { name: 'Log an observation' }),
    ).toBeTruthy();
    expect(screen.getByRole('radio', { name: 'Ketchup' })).toBeTruthy();
    await fireEvent.press(screen.getByRole('radio', { name: 'Ketchup' }));
    await fireEvent.changeText(screen.getByLabelText('Reaction'), 'Heartburn');
    await fireEvent.press(
      screen.getByRole('button', { name: 'Save observation' }),
    );
    expect(
      await screen.findByRole('header', { name: 'Observation recorded' }),
    ).toBeTruthy();

    await fireEvent.press(screen.getByRole('button', { name: 'Back to foods' }));
    expect(await screen.findByRole('header', { name: 'Foods' })).toBeTruthy();
  });
});
