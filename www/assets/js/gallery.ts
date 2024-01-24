import { hideLoading, showLoading } from "./loading";

async function gallery(page: number = 1): Promise<void> {
  showLoading();
  try {
    const current_page: number = page; // the current page
    const pictures_per_page: number = 10; // 2 pictures per page
    const max_num_of_pages: number = 5; // max number of pages to show in the pagination bar

    const response = await fetch(`/api/GetPhotos?page=${current_page}`); // replace with your API endpoint
    const data = await response.json();

    const total_num_of_pictures: number = data.NumPictures; // total number of pictures
    $("#pages2").html("");
    $("#pages2").append(
      `<div style="width: 100%;"> Número total de fotos: ${total_num_of_pictures} </div>`
    );

    $("#pages").html("");
    const num_of_pages: number = Math.ceil(
      total_num_of_pictures / pictures_per_page
    );

    let inicial: number = 1;
    let final: number = num_of_pages;

    if (num_of_pages > max_num_of_pages) {
      if (current_page > 1) {
        $("#pages").append(`<button class="page" id="0" ><</button>`);
      }

      if (current_page > Math.floor(max_num_of_pages / 2)) {
        inicial = current_page - Math.floor(max_num_of_pages / 2);
        if (current_page + Math.floor(max_num_of_pages / 2) <= num_of_pages) {
          final = current_page + Math.floor(max_num_of_pages / 2);
          inicial = final - max_num_of_pages + 1;
        }
      } else {
        final = max_num_of_pages;
      }
    }

    for (let i = inicial; i <= final; i++) {
      if (i == current_page) {
        $("#pages").append(
          `<button class="pageNumber selected" id="${i}" >${i}</button>`
        );
      } else {
        $("#pages").append(
          `<button class="pageNumber" id="${i}" >${i}</button>`
        );
      }
    }

    if (num_of_pages > max_num_of_pages && current_page < num_of_pages) {
      $("#pages").append(`<button class="page" id="1000" >></button>`);
    }

    // Add click event listeners to the page buttons
    $(".pageNumber").on("click", function () {
      $(".pageNumber").off("click"); // remove all the clicks before starting anew
      let next_page: number = parseInt($(this).attr("id"));
      if (next_page === 0) {
        next_page = current_page - 1;
      } else if (next_page == 1000) {
        next_page = current_page + 1;
      }
      gallery(next_page);
    });

    let i: number = 0;
    $("#mygallery").html("");
    data.Pictures.forEach((element: any) => {
      i++;
      //add elements to mygallery
      $("#mygallery").append(`<div>
      <a href="${element.Uri}" target="_blank">
      <img class="grid-item grid-item-${i}" src="${element.ThumbnailUri}" alt="${element.Name}" />
      </a>
      <p>${element.Name}</p>
      </div>`);
    });
  } catch (error) {
    console.error("Error:", error);
  } finally {
    hideLoading();
  }
}

$(async () => {
  await gallery();
});
