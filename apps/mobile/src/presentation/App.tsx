import { NavigationContainer } from '@react-navigation/native';

import { FoodApiClient } from '../infrastructure/api/FoodApiClient';
import { apiBaseUrl } from '../infrastructure/api/apiBaseUrl';
import { RootNavigator } from './navigation/RootNavigator';

const foodRepository = new FoodApiClient(apiBaseUrl);

export default function App() {
  return (
    <NavigationContainer>
      <RootNavigator foodRepository={foodRepository} />
    </NavigationContainer>
  );
}
