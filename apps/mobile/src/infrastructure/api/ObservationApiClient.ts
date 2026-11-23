import type { CreateObservationRequest } from '../../application/observations/CreateObservationRequest';
import type { Observation } from '../../application/observations/Observation';
import type { ObservationRepository } from '../../application/observations/ObservationRepository';

export class ObservationApiError extends Error {}

export class ObservationApiClient implements ObservationRepository {
  private readonly baseUrl?: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl?.replace(/\/+$/, '');
  }

  async createObservation(
    request: CreateObservationRequest,
  ): Promise<Observation> {
    const response = await this.request(request);

    if (!response.ok) {
      throw new ObservationApiError(await this.getErrorMessage(response));
    }

    return (await response.json()) as Observation;
  }

  private async request(
    observation: CreateObservationRequest,
  ): Promise<Response> {
    if (!this.baseUrl) {
      throw new ObservationApiError(
        'Set EXPO_PUBLIC_API_BASE_URL to connect to the Triggered API.',
      );
    }

    try {
      return await fetch(`${this.baseUrl}/api/observations`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(observation),
      });
    } catch {
      throw new ObservationApiError(
        'Could not connect to the server. Check your connection and try again.',
      );
    }
  }

  private async getErrorMessage(response: Response): Promise<string> {
    if (response.status === 400) {
      try {
        const problem = (await response.json()) as {
          errors?: Record<string, string[]>;
        };
        const validationMessage = Object.values(problem.errors ?? {})
          .flat()
          .find((message) => message.trim().length > 0);

        return (
          validationMessage ??
          'Check the observation details and try again.'
        );
      } catch {
        return 'Check the observation details and try again.';
      }
    }

    if (response.status === 404) {
      return 'That food could not be found. Choose a saved food and try again.';
    }

    return response.status >= 500
      ? 'The server could not save the observation. Please try again.'
      : 'The observation could not be saved. Please try again.';
  }
}
