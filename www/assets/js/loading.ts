const loading = document.querySelector("#loading-panel");

export function showLoading(timeout: number = 0) {
  loading.classList.remove("hidden");
  if (timeout > 0) {
    setTimeout(() => {
      hideLoading();
    }, timeout);
  }
}

export function hideLoading() {
  loading.classList.add("hidden");
}
