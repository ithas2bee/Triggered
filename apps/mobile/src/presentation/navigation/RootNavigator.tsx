import { createNativeStackNavigator } from '@react-navigation/native-stack';

import type { RootStackParamList } from '../../application/navigation/routes';
import { HomeScreen } from '../screens/HomeScreen';
import { PlaceholderScreen } from '../screens/PlaceholderScreen';

const Stack = createNativeStackNavigator<RootStackParamList>();

export function RootNavigator() {
  return (
    <Stack.Navigator initialRouteName="Home">
      <Stack.Screen
        name="Home"
        component={HomeScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen name="FoodList">
        {() => <PlaceholderScreen title="Food list" />}
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
