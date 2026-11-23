import { createNativeStackNavigator } from '@react-navigation/native-stack';

import type { RootStackParamList } from '../../application/navigation/routes';
import type { FoodRepository } from '../../application/foods/FoodRepository';
import { FoodListScreen } from '../screens/FoodListScreen';
import { HomeScreen } from '../screens/HomeScreen';
import { PlaceholderScreen } from '../screens/PlaceholderScreen';

const Stack = createNativeStackNavigator<RootStackParamList>();

export function RootNavigator({
  foodRepository,
}: {
  foodRepository: FoodRepository;
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
      <Stack.Screen name="ObservationEntry">
        {() => <PlaceholderScreen title="Observation entry" />}
      </Stack.Screen>
    </Stack.Navigator>
  );
}
