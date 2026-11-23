import { useEffect, useRef, useState } from 'react';
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
import type { ObservationRepository } from '../../application/observations/ObservationRepository';
import type { RootStackParamList } from '../../application/navigation/routes';

type ObservationEntryScreenProps = {
  foodRepository: FoodRepository;
  observationRepository: ObservationRepository;
};

type ObservationFieldErrors = {
  foodId?: string;
  consumedAt?: string;
  reaction?: string;
};

type ObservationNavigation = NativeStackNavigationProp<
  RootStackParamList,
  'ObservationEntry'
>;

export function ObservationEntryScreen({
  foodRepository,
  observationRepository,
}: ObservationEntryScreenProps) {
  const navigation = useNavigation<ObservationNavigation>();
  const [foods, setFoods] = useState<Food[]>([]);
  const [selectedFoodId, setSelectedFoodId] = useState('');
  const [date, setDate] = useState(getLocalDate());
  const [time, setTime] = useState(getLocalTime());
  const [reaction, setReaction] = useState('');
  const [fieldErrors, setFieldErrors] = useState<ObservationFieldErrors>({});
  const [loadError, setLoadError] = useState('');
  const [submitError, setSubmitError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdObservation, setCreatedObservation] = useState<{
    foodName: string;
    reaction: string;
  }>();
  const submissionInProgress = useRef(false);

  async function loadFoods() {
    setIsLoading(true);
    setLoadError('');

    try {
      const savedFoods = await foodRepository.getFoods();
      setFoods(savedFoods);
      setSelectedFoodId((current) =>
        savedFoods.some((food) => food.id === current)
          ? current
          : '',
      );
    } catch (error) {
      setLoadError(getErrorMessage(error, 'Foods could not be loaded.'));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadFoods();
  }, []);

  async function submitObservation() {
    if (submissionInProgress.current) {
      return;
    }

    const selectedFood = foods.find((food) => food.id === selectedFoodId);
    const consumedAt = toUtcIsoDateTime(date, time);
    const nextErrors: ObservationFieldErrors = {};

    if (!selectedFood) {
      nextErrors.foodId = 'Select a saved food.';
    }
    if (!consumedAt) {
      nextErrors.consumedAt = 'Enter a valid date and time.';
    }
    if (!reaction.trim()) {
      nextErrors.reaction = 'Enter the reaction you experienced.';
    }

    setFieldErrors(nextErrors);
    setSubmitError('');

    if (Object.keys(nextErrors).length > 0 || !selectedFood || !consumedAt) {
      return;
    }

    submissionInProgress.current = true;
    setIsSubmitting(true);

    try {
      const observation = await observationRepository.createObservation({
        foodId: selectedFood.id,
        consumedAt,
        reaction,
      });
      setCreatedObservation({
        foodName: selectedFood.name,
        reaction: observation.reaction,
      });
    } catch (error) {
      setSubmitError(
        getErrorMessage(error, 'The observation could not be saved.'),
      );
    } finally {
      submissionInProgress.current = false;
      setIsSubmitting(false);
    }
  }

  if (createdObservation) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.content}>
          <Text accessibilityRole="header" style={styles.title}>
            Observation recorded
          </Text>
          <Text accessibilityRole="alert" style={styles.description}>
            Recorded {createdObservation.reaction} for{' '}
            {createdObservation.foodName}.
          </Text>
          <ActionButton
            label="Back to foods"
            onPress={() => navigation.goBack()}
          />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text accessibilityRole="header" style={styles.title}>
          Log an observation
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
            <ActionButton label="Retry loading foods" onPress={() => void loadFoods()} />
          </View>
        ) : foods.length === 0 ? (
          <View>
            <Text style={styles.description}>
              Add a food before logging an observation.
            </Text>
            <ActionButton
              label="Back to foods"
              onPress={() => navigation.goBack()}
              secondary
            />
          </View>
        ) : (
          <>
            <Text style={styles.label}>Food</Text>
            {foods.map((food) => {
              const isSelected = selectedFoodId === food.id;

              return (
                <Pressable
                  accessibilityLabel={food.name}
                  accessibilityRole="radio"
                  accessibilityState={{ checked: isSelected }}
                  disabled={isSubmitting}
                  key={food.id}
                  onPress={() => {
                    setSelectedFoodId(food.id);
                    setFieldErrors((current) => ({
                      ...current,
                      foodId: undefined,
                    }));
                  }}
                  style={[
                    styles.foodOption,
                    isSelected && styles.selectedFoodOption,
                  ]}
                >
                  <Text style={styles.foodName}>
                    {food.name}
                    {isSelected ? ' (selected)' : ''}
                  </Text>
                </Pressable>
              );
            })}
            {fieldErrors.foodId ? (
              <Text accessibilityRole="alert" style={styles.error}>
                {fieldErrors.foodId}
              </Text>
            ) : null}

            <Text style={styles.label}>Consumption date and time</Text>
            <Text style={styles.hint}>
              Enter local time in YYYY-MM-DD and 24-hour HH:mm format.
            </Text>
            <TextInput
              accessibilityLabel="Consumption date"
              editable={!isSubmitting}
              onChangeText={setDate}
              placeholder="YYYY-MM-DD"
              style={styles.input}
              value={date}
            />
            <TextInput
              accessibilityLabel="Consumption time"
              editable={!isSubmitting}
              onChangeText={setTime}
              placeholder="HH:mm"
              style={styles.input}
              value={time}
            />
            {fieldErrors.consumedAt ? (
              <Text accessibilityRole="alert" style={styles.error}>
                {fieldErrors.consumedAt}
              </Text>
            ) : null}

            <Text style={styles.label}>Reaction</Text>
            <TextInput
              accessibilityLabel="Reaction"
              editable={!isSubmitting}
              onChangeText={(value) => {
                setReaction(value);
                setFieldErrors((current) => ({
                  ...current,
                  reaction: undefined,
                }));
              }}
              placeholder="Describe what you experienced"
              style={styles.input}
              value={reaction}
            />
            {fieldErrors.reaction ? (
              <Text accessibilityRole="alert" style={styles.error}>
                {fieldErrors.reaction}
              </Text>
            ) : null}

            {submitError ? (
              <Text accessibilityRole="alert" style={styles.error}>
                {submitError}
              </Text>
            ) : null}
            <ActionButton
              disabled={isSubmitting}
              label={isSubmitting ? 'Saving observation…' : 'Save observation'}
              onPress={() => void submitObservation()}
            />
            <ActionButton
              disabled={isSubmitting}
              label="Cancel"
              onPress={() => navigation.goBack()}
              secondary
            />
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

function getLocalDate(): string {
  const now = new Date();
  const year = String(now.getFullYear()).padStart(4, '0');
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function getLocalTime(): string {
  const now = new Date();
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');

  return `${hours}:${minutes}`;
}

function toUtcIsoDateTime(date: string, time: string): string | undefined {
  const dateParts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
  const timeParts = /^([01]\d|2[0-3]):([0-5]\d)$/.exec(time);

  if (!dateParts || !timeParts) {
    return undefined;
  }

  const [, yearText, monthText, dayText] = dateParts;
  const [, hoursText, minutesText] = timeParts;
  const year = Number(yearText);
  const month = Number(monthText);
  const day = Number(dayText);
  const hours = Number(hoursText);
  const minutes = Number(minutesText);
  const localDate = new Date(year, month - 1, day, hours, minutes);

  if (
    localDate.getFullYear() !== year ||
    localDate.getMonth() !== month - 1 ||
    localDate.getDate() !== day ||
    localDate.getHours() !== hours ||
    localDate.getMinutes() !== minutes ||
    (year === 1 && month === 1 && day === 1 && hours === 0 && minutes === 0)
  ) {
    return undefined;
  }

  return localDate.toISOString();
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
  label: {
    color: '#183153',
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 8,
    marginTop: 16,
  },
  hint: {
    color: '#526174',
    fontSize: 14,
    marginBottom: 8,
  },
  foodOption: {
    borderColor: '#9aa8b8',
    borderRadius: 8,
    borderWidth: 1,
    marginBottom: 8,
    padding: 12,
  },
  selectedFoodOption: {
    borderColor: '#183153',
    backgroundColor: '#e8edf2',
  },
  foodName: {
    color: '#183153',
    fontSize: 16,
  },
  input: {
    borderColor: '#9aa8b8',
    borderRadius: 8,
    borderWidth: 1,
    color: '#183153',
    fontSize: 16,
    marginBottom: 8,
    padding: 12,
  },
  status: {
    marginTop: 16,
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
