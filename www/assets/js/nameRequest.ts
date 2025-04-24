import { getUserInfo } from "./userInfo";

// create a cookie named 'nameRequest' with a string value that is converted to a Base64 string when stored
// and decoded when read. Use plain TypeScript and no external libraries.
// The cookie should expire in 30 days

function setCookie(name: string, value: string, days: number) {
  const date = new Date();
  date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
  const expires = "expires=" + date.toUTCString();
  document.cookie = name + "=" + btoa(value) + ";" + expires + ";path=/";
}
function getCookie(name: string) {
  const nameEQ = name + "=";
  const ca = document.cookie.split(";");
  for (let i = 0; i < ca.length; i++) {
    let c = ca[i];
    while (c.charAt(0) === " ") c = c.substring(1, c.length);
    if (c.indexOf(nameEQ) === 0)
      return atob(c.substring(nameEQ.length, c.length));
  }
  return null;
}
function eraseCookie(name: string) {
  document.cookie = name + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
}
function getNameRequest() {
  const nameRequest = getCookie("nameRequest");
  if (nameRequest) {
    return JSON.parse(nameRequest);
  }
  return null;
}
function setNameRequest(nameRequest: any) {
  setCookie("nameRequest", JSON.stringify(nameRequest), 30);
}
function eraseNameRequest() {
  eraseCookie("nameRequest");
}

(async () => {
  const modal = document.getElementById("name-request-modal");
  const nameRequestForm = document.getElementById("name-request-form");
  nameRequestForm.addEventListener("submit", function (event) {
    event.preventDefault();
    const formData = new FormData(event.target as HTMLFormElement);
    const nameRequest = formData.get("name-request");
    if (!nameRequest) {
      return;
    }

    if (nameRequest.toString().length > 2) {
      setNameRequest(nameRequest);
      document.getElementById("name-request-name").innerText =
        nameRequest.toString();

      modal.style.display = "none";
    }
  });
  nameRequestForm.addEventListener("reset", function () {
    eraseNameRequest();
  });

  if (nameRequestForm) {
    const nameInput = nameRequestForm.querySelector<HTMLInputElement>(
      'input[name="name-request"]'
    );
    const userInfo = await getUserInfo();
    if (userInfo?.userDetails) {
      nameInput.value = userInfo.userDetails;
    } else {
      var currentName = getNameRequest();
      if (currentName) {
        nameInput.value = currentName;
      } else {
        // Show the form in a modal dialog
        modal.style.display = "block";
      }
    }
    document.getElementById("name-request-name").innerText = nameInput.value;
  } else {
    alert("bad page");
  }
})();
