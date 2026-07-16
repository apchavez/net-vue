// jsdom does not implement ResizeObserver, which Vuetify components (e.g. VProgressCircular) use.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

globalThis.ResizeObserver ??=
  ResizeObserverStub as unknown as typeof ResizeObserver;
