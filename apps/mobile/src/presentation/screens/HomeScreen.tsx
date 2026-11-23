import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { RootStackParamList } from '../../application/navigation/routes';

type HomeNavigation = NativeStackNavigationProp<RootStackParamList, 'Home'>;

export function HomeScreen() {
  const navigation = useNavigation<HomeNavigation>();

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <Text accessibilityRole="header" style={styles.title}>
          Triggered
        </Text>
        <Text style={styles.subtitle}>Your personal food and reaction journal</Text>
        <Pressable
          accessibilityRole="button"
          onPress={() => navigation.navigate('FoodList')}
          style={styles.button}
        >
          <Text style={styles.buttonText}>Food list</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={() => navigation.navigate('ObservationEntry')}
          style={styles.button}
        >
          <Text style={styles.buttonText}>Observation entry</Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#ffffff',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  title: {
    color: '#183153',
    fontSize: 32,
    fontWeight: '700',
  },
  subtitle: {
    color: '#526174',
    fontSize: 16,
    marginBottom: 24,
    marginTop: 8,
  },
  button: {
    alignItems: 'center',
    backgroundColor: '#183153',
    borderRadius: 8,
    marginTop: 12,
    padding: 14,
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '600',
  },
});
