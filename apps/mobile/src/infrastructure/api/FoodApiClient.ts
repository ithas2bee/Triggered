import type { Food } from '../../application/foods/Food';
import type { FoodRepository } from '../../application/foods/FoodRepository';

export class FoodApiError extends Error {}

export class FoodApiClient implements FoodRepository {
  private readonly baseUrl?: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl?.replace(/\/+$/, '');
  }

  async getFoods(): Promise<Food[]> {
    const response = await this.request('/api/foods');

    if (!response.ok) {
      throw new FoodApiError('Foods could not be loaded. Please try again.');
    }

    return (await response.json()) as Food[];
  }

  async createFood(name: string): Promise<Food> {
    const response = await this.request('/api/foods', {
      method: 'POST',
      body: JSON.stringify({ name }),
    });

    if (!response.ok) {
      throw new FoodApiError(await this.getCreateErrorMessage(response));
    }

    return (await response.json()) as Food;
  }

  private async request(path: string, init?: RequestInit): Promise<Response> {
    if (!this.baseUrl) {
      throw new FoodApiError(
        'Set EXPO_PUBLIC_API_BASE_URL to connect to the Triggered API.',
      );
    }

    try {
      return await fetch(`${this.baseUrl}${path}`, {
        ...init,
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          ...init?.headers,
        },
      });
    } catch {
      throw new FoodApiError(
        'Could not connect to the server. Check your connection and try again.',
      );
    }
  }

  private async getCreateErrorMessage(response: Response): Promise<string> {
    if (response.status === 400) {
      try {
        const problem = (await response.json()) as {
          errors?: Record<string, string[]>;
        };
        const validationMessage = Object.values(problem.errors ?? {})
          .flat()
          .find((message) => message.trim().length > 0);

        return validationMessage ?? 'Enter a valid food name and try again.';
      } catch {
        return 'Enter a valid food name and try again.';
      }
    }

    return response.status >= 500
      ? 'The server could not save the food. Please try again.'
      : 'The food could not be saved. Please try again.';
  }
}
