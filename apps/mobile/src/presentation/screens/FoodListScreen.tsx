import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import type { Food } from '../../application/foods/Food';
import type { FoodRepository } from '../../application/foods/FoodRepository';
import type { RootStackParamList } from '../../application/navigation/routes';

type FoodListScreenProps = {
  foodRepository: FoodRepository;
};

type FoodListNavigation = NativeStackNavigationProp<
  RootStackParamList,
  'FoodList'
>;

export function FoodListScreen({ foodRepository }: FoodListScreenProps) {
  const navigation = useNavigation<FoodListNavigation>();
  const [foods, setFoods] = useState<Food[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [foodName, setFoodName] = useState('');
  const [createError, setCreateError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadFoods() {
    setIsLoading(true);
    setLoadError('');

    try {
      setFoods(await foodRepository.getFoods());
    } catch (error) {
      setLoadError(getErrorMessage(error, 'Foods could not be loaded.'));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadFoods();
  }, []);

  async function submitFood() {
    if (!foodName.trim() || isSubmitting) {
      return;
    }

    setIsSubmitting(true);
    setCreateError('');

    try {
      const food = await foodRepository.createFood(foodName);
      setFoods((currentFoods) => [food, ...currentFoods]);
      setFoodName('');
      setIsCreating(false);
    } catch (error) {
      setCreateError(getErrorMessage(error, 'The food could not be saved.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text accessibilityRole="header" style={styles.title}>
          Foods
        </Text>

        {isLoading ? (
          <ActivityIndicator
            accessibilityLabel="Loading foods"
            style={styles.status}
          />
        ) : loadError ? (
          <View>
            <Text accessibilityRole="alert" style={styles.error}>
              {loadError}
            </Text>
            <ActionButton label="Retry" onPress={() => void loadFoods()} />
          </View>
        ) : (
          <>
            {foods.length === 0 ? (
              <Text style={styles.description}>
                No foods yet. Add a food to get started.
              </Text>
            ) : (
              foods.map((food) => (
                <Text key={food.id} style={styles.foodName}>
                  {food.name}
                </Text>
              ))
            )}

            {isCreating ? (
              <View style={styles.form}>
                <TextInput
                  accessibilityLabel="Food name"
                  autoCapitalize="words"
                  editable={!isSubmitting}
                  onChangeText={(value) => {
                    setFoodName(value);
                    setCreateError('');
                  }}
                  placeholder="Food name"
                  style={styles.input}
                  value={foodName}
                />
                {createError ? (
                  <Text accessibilityRole="alert" style={styles.error}>
                    {createError}
                  </Text>
                ) : null}
                <ActionButton
                  disabled={isSubmitting || !foodName.trim()}
                  label={isSubmitting ? 'Saving…' : 'Save food'}
                  onPress={() => void submitFood()}
                />
                <ActionButton
                  label="Cancel"
                  onPress={() => {
                    setIsCreating(false);
                    setCreateError('');
                  }}
                  secondary
                />
              </View>
            ) : (
              <View>
                <ActionButton
                  label="Add a food"
                  onPress={() => setIsCreating(true)}
                />
                <ActionButton
                  label="Log an observation"
                  onPress={() => navigation.navigate('ObservationEntry')}
                  secondary
                />
              </View>
            )}
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

function ActionButton({
  disabled = false,
  label,
  onPress,
  secondary = false,
}: {
  disabled?: boolean;
  label: string;
  onPress: () => void;
  secondary?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      onPress={onPress}
      style={[
        styles.button,
        secondary && styles.secondaryButton,
        disabled && styles.disabledButton,
      ]}
    >
      <Text style={[styles.buttonText, secondary && styles.secondaryButtonText]}>
        {label}
      </Text>
    </Pressable>
  );
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#ffffff',
  },
  content: {
    flexGrow: 1,
    padding: 24,
  },
  title: {
    color: '#183153',
    fontSize: 28,
    fontWeight: '700',
    marginBottom: 16,
  },
  description: {
    color: '#526174',
    fontSize: 16,
    marginBottom: 12,
  },
  foodName: {
    color: '#183153',
    fontSize: 16,
    paddingVertical: 12,
  },
  status: {
    marginTop: 16,
  },
  form: {
    marginTop: 16,
  },
  input: {
    borderColor: '#9aa8b8',
    borderRadius: 8,
    borderWidth: 1,
    color: '#183153',
    fontSize: 16,
    padding: 12,
  },
  error: {
    color: '#a12622',
    fontSize: 14,
    marginTop: 8,
  },
  button: {
    alignItems: 'center',
    backgroundColor: '#183153',
    borderRadius: 8,
    marginTop: 12,
    padding: 14,
  },
  secondaryButton: {
    backgroundColor: '#e8edf2',
  },
  disabledButton: {
    opacity: 0.55,
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '600',
  },
  secondaryButtonText: {
    color: '#183153',
  },
});
