const now = new Date();
const future = new Date('2025-05-03');
future.setHours(14);

const diff = Math.abs(future - now);
const days = Math.floor(diff / (1000 * 60 * 60 * 24));
const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

console.log(`There are ${days} days and ${hours} hours until May 3, 2025.`);
document.querySelector('[id="days"]').innerHTML = days
document.querySelector('[id="hours"]').innerHTML = hours








