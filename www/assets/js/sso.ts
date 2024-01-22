(() => {
  if (window.location.search.endsWith(".referrer")) {
    console.log("Referrer replace failed, redirecting to main page");
  } else {
    var links = document.querySelectorAll<HTMLLinkElement>("ul#sso li a");
    links.forEach((link) => {
      link.href = link.href + window.location.search;
    });
  }
})();
