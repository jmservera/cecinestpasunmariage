// call api

async function gallery(page = 1) {
  try {

    var current_page = page; // the current page
    var pictures_per_page = 10; // 2 pictures per page
    var max_num_of_pages = 5; // max number of pages to show in the pagination bar
    

    const response = await fetch(`/api/GetPhotos?page=${current_page}`); // replace with your API endpoint
    const data = await response.json();

    var total_num_of_pictures = data.NumPictures; // total number of pictures
    $("#pages2").html('');
    $("#pages2").append(`<div style="width: 100%;"> Número total de fotos: ${total_num_of_pictures} </div>`);

    $("#pages").html('');
    var num_of_pages = Math.ceil(total_num_of_pictures / pictures_per_page);

    var inicial = 1;
    var final = num_of_pages;

    if(num_of_pages > max_num_of_pages)
    {
      if(current_page > 1)
      {
        $("#pages").append(`<button class="page" id="0" ><</button>`);
      }
      
      if( current_page > Math.floor(max_num_of_pages/2) )
      {
        inicial = current_page - Math.floor(max_num_of_pages/2);
        if(current_page + Math.floor(max_num_of_pages/2) <= num_of_pages)
        {
          final = current_page + Math.floor(max_num_of_pages/2);
          inicial = final - max_num_of_pages+1;
        }
      }
      else
      {
        final = max_num_of_pages;
      }           
    }
    
    for (var i = inicial; i <= final; i++) {
      if(i == current_page)
      {
        $("#pages").append(`<button class="page selected" id="${i}" >${i}</button>`);
      }
      else
      {
          $("#pages").append(`<button class="page" id="${i}" >${i}</button>`);
      }
    }

    if(num_of_pages > max_num_of_pages && current_page < num_of_pages)
    {
      $("#pages").append(`<button class="page" id="1000" >></button>`);
    }
    
      // Add click event listeners to the page buttons
      $(".page").click(function() {
        $('.page').off('click'); // remove all the clicks before starting anew
        next_page = parseInt($(this).attr('id'));
        if(next_page == 0)
        {
          next_page = current_page-1;
        }
        else if(next_page == 1000)
        {
          next_page = current_page+1;
        }
        gallery(next_page);
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
