let cookieLang:string;

document.cookie.split(";").forEach(function(cookie) {
  if(cookie.includes("lang=")) {
    cookieLang=cookie.split("=")[1].trim();
  }
});

if(window.lang !== cookieLang ) {  
  let path:string = document.location.pathname;
  let newpath:string = path;

  if(document.location.pathname[3]==="/"){
    path = document.location.pathname.slice(3);
  }

  if(cookieLang!=="es"){
    newpath="/"+cookieLang+path;
  }

  if(document.location.pathname !== newpath){
    document.location.pathname=newpath;
  }
}