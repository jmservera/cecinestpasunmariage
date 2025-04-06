function showStatus(message: string, className: string = "") {
  var statusElement = document.getElementById("__statusSpan");
  if (statusElement === null) {
    console.error("Status element not found, creating a new one");
    statusElement = document.createElement("span");
    statusElement.id = "__statusSpan";
    statusElement.className = `status ${className}`; // Apply success styling
    statusElement.style.display = "none"; // Initially hide the status element
    document.body.appendChild(statusElement);
  }
  statusElement.textContent = message;
  statusElement.className = `status ${className}`; // Apply success styling
  statusElement.style.display = "block";
  setTimeout(() => {
    statusElement.textContent = "";
    statusElement.className = "status"; // Clear the status styling
    statusElement.style.display = "none"; // Hide the status element
  }, 5000);
}

var statusElement = document.getElementById("__statusSpan");
if (statusElement === null) {
  console.error("Status element not found, creating a new one");
  statusElement = document.createElement("span");
  statusElement.id = "__statusSpan";
  statusElement.className = "status"; // Apply success styling
  statusElement.style.display = "none"; // Initially hide the status element
  document.body.appendChild(statusElement);
}

export { showStatus };
