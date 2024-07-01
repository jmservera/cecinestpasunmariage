let search: string = window.location.search;
if (search.endsWith(".referrer")) {
  console.log("Referrer replace failed, getting from local storage");
  const url: string = localStorage.getItem("lastPage");
  if (url && url !== undefined) {
    search = search.replace(".referrer", url);
  } else {
    search = "";
  }
}
var links = document.querySelectorAll<HTMLLinkElement>("ul#sso li a");
links.forEach((link) => {
  link.href = link.href + search;
});

