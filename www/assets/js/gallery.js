// call api

async function gallery(page = 1) {
  try {

    var current_page = page; // the current page
    var pictures_per_page = 10; // 2 pictures per page
    

    const response = await fetch(`/api/GetPhotos?page=${current_page}`); // replace with your API endpoint
    const data = await response.json();

    var total_num_of_pictures = data.NumPictures; // total number of pictures
    $("#pages2").html('');
    $("#pages2").append(`<div style="width: 100%;"> Total number of pictures: ${total_num_of_pictures} </div>`);

    $("#pages").html('');
    for (var i = 1; i <= Math.ceil(total_num_of_pictures / pictures_per_page); i++) {
      $("#pages").append(`<button class="page" id="${i}" >${i}</button>`);
    }

      // Add click event listeners to the page buttons
      $(".page").click(function() {
        $('.page').off('click'); // remove all the clicks before starting anew
        gallery($(this).attr('id'));
      });
    
    var i = 0;
    $("#mygallery").html('');
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
