export type RootStackParamList = {
  Home: undefined;
  FoodList: undefined;
  FoodDetail: { foodId: string };
  ObservationEntry: { foodId?: string } | undefined;
};
