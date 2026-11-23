import type { CreateObservationRequest } from './CreateObservationRequest';
import type { Observation } from './Observation';

export interface ObservationRepository {
  createObservation(
    request: CreateObservationRequest,
  ): Promise<Observation>;
}
