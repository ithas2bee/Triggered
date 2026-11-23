import { fireEvent, render } from '@testing-library/react-native';

import App from '../src/presentation/App';

describe('mobile app', () => {
  it('renders the Triggered home screen at startup', async () => {
    const { getByRole, getByText } = await render(<App />);

    expect(getByRole('header', { name: 'Triggered' })).toBeTruthy();
    expect(getByText('Your personal food and reaction journal')).toBeTruthy();
  });

  it('navigates from home to the observation entry placeholder', async () => {
    const { getByRole } = await render(<App />);

    await fireEvent.press(getByRole('button', { name: 'Observation entry' }));

    expect(getByRole('header', { name: 'Observation entry' })).toBeTruthy();
  });
});
