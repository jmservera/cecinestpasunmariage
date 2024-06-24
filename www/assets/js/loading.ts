const loading = document.querySelector("#loading-panel");

function showLoading(timeout: number = 0) {
  loading.classList.remove("hidden");
  if (timeout > 0) {
    setTimeout(() => {
      hideLoading();
    }, timeout);
  }
}

function hideLoading() {
  loading.classList.add("hidden");
}

export { showLoading, hideLoading };