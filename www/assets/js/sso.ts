(() => {
  var links = document.querySelectorAll<HTMLLinkElement>("ul#sso li a");
  links.forEach((link) => {
    link.href = link.href + window.location.search;
  });
})();
