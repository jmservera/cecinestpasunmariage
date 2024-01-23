const loading = document.querySelector("#loading-panel");

export function showLoading() {
  loading.classList.remove("hidden");
}

export function hideLoading() {
  loading.classList.add("hidden");
}
