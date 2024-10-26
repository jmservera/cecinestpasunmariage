import { getUserInfo } from "./userInfo";

getUserInfo().then((info) => {
  const hello = document.querySelector<HTMLSpanElement>(
    "div#loggedUser span#hello",
  );
  const logout = document.querySelector<HTMLAnchorElement>(
    "div#loggedUser a#logout",
  );
  const login = document.querySelector<HTMLAnchorElement>(
    "div#loggedUser a#login",
  );

  if (info) {
    const username = document.querySelector<HTMLSpanElement>(
      "div#loggedUser span#username",
    );
    username.innerText = info.userDetails;
    hello.style.display = "";
    logout.style.display = "";
    login.style.display = "none";
  } else {
    hello.style.display = "none";
    logout.style.display = "none";
    login.style.display = "";
  }
});

window.onbeforeunload = function () {
  const active = document.activeElement as HTMLAnchorElement;
  if (active.href) {
    localStorage.setItem("lastPage", active.href);
  } else {
    localStorage.setItem("lastPage", window.location.href);
  }
};
