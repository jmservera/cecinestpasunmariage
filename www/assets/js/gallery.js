// call api

async function gallery() {
  try {
    const response = await fetch("/api/GetPhotos?page=1"); // replace with your API endpoint
    const data = await response.json();

    var i = 0;
    data.Pictures.forEach((element) => {
      i++;
      //add elements to mygallery
      $("#mygallery").append(`<div>
      <img class="grid-item grid-item-${i}" src="${element.Uri}" alt="${element.Name}" />
      <p>${element.Name}</p>
      </div>`);
    });
  } catch (error) {
    console.error("Error:", error);
  }
}

$(document).ready(async () => {
  await gallery();
});
