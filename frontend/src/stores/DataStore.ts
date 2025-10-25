import { makeAutoObservable, runInAction } from "mobx";
import { api } from "../services/api";

export interface Product {
  id: string;
  name: string;
  price: number;
  description: string;
}

export class DataStore {
  products: Product[] = [];
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  async fetchProducts(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.get("/products");

      runInAction(() => {
        this.products = response.data;
        this.loading = false;
      });
    } catch (error: unknown) {
      runInAction(() => {
        const message =
          error instanceof Error
            ? error.message
            : (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
              "Failed to fetch products";
        this.error = message;
        this.loading = false;
      });
    }
  }
}
