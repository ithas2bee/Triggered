import { NavigationContainer } from '@react-navigation/native';

import { FoodApiClient } from '../infrastructure/api/FoodApiClient';
import { ObservationApiClient } from '../infrastructure/api/ObservationApiClient';
import { apiBaseUrl } from '../infrastructure/api/apiBaseUrl';
import { RootNavigator } from './navigation/RootNavigator';

const foodRepository = new FoodApiClient(apiBaseUrl);
const observationRepository = new ObservationApiClient(apiBaseUrl);

export default function App() {
  return (
    <NavigationContainer>
      <RootNavigator
        foodRepository={foodRepository}
        observationRepository={observationRepository}
      />
    </NavigationContainer>
  );
}
