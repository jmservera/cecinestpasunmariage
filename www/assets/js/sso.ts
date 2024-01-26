let search: string = window.location.search;
if (search.endsWith(".referrer")) {
  console.log("Referrer replace failed, getting from local storage");
  const url: string = localStorage.getItem("lastPage");
  if (url) {
    search = search.replace(".referrer", url);
  }
  else {
    search = search.replace(".referrer", "/");
  }
}
var links = document.querySelectorAll<HTMLLinkElement>("ul#sso li a");
links.forEach((link) => {
  link.href = link.href + search;
});
