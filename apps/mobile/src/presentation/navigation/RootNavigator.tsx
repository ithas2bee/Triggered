import { createNativeStackNavigator } from '@react-navigation/native-stack';

import type { RootStackParamList } from '../../application/navigation/routes';
import type { FoodRepository } from '../../application/foods/FoodRepository';
import type { ObservationRepository } from '../../application/observations/ObservationRepository';
import { FoodListScreen } from '../screens/FoodListScreen';
import { HomeScreen } from '../screens/HomeScreen';
import { ObservationEntryScreen } from '../screens/ObservationEntryScreen';
import { PlaceholderScreen } from '../screens/PlaceholderScreen';

const Stack = createNativeStackNavigator<RootStackParamList>();

export function RootNavigator({
  foodRepository,
  observationRepository,
}: {
  foodRepository: FoodRepository;
  observationRepository: ObservationRepository;
}) {
  return (
    <Stack.Navigator initialRouteName="Home">
      <Stack.Screen
        name="Home"
        component={HomeScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen name="FoodList" options={{ title: 'Foods' }}>
        {() => <FoodListScreen foodRepository={foodRepository} />}
      </Stack.Screen>
      <Stack.Screen name="FoodDetail">
        {() => <PlaceholderScreen title="Food detail and history" />}
      </Stack.Screen>
      <Stack.Screen
        name="ObservationEntry"
        options={{ title: 'Log observation' }}
      >
        {() => (
          <ObservationEntryScreen
            foodRepository={foodRepository}
            observationRepository={observationRepository}
          />
        )}
      </Stack.Screen>
    </Stack.Navigator>
  );
}
