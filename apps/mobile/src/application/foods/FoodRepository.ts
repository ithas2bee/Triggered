import type { Food } from './Food';

export interface FoodRepository {
  getFoods(): Promise<Food[]>;
  createFood(name: string): Promise<Food>;
}
