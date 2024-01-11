function runCountdown(future) {
  const now = new Date();
  const diff = Math.abs(future - now);

  function calculateTimeDifference() {
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    return { days, hours };
  }

  function updateDate() {
    const { days, hours } = calculateTimeDifference();
    console.log(`There are ${days} days and ${hours} hours until ${future}.`);

    document.querySelector('.countdown [id="d"]').innerHTML = days;
    document.querySelector('.countdown [id="h"]').innerHTML = hours;
  }

  setInterval(updateDate, 60000);
  updateDate();
}

// disable minification for the following function
// todo: use proper modules
window.runCountdown = runCountdown;
