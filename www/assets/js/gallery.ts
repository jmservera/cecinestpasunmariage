import { hideLoading, showLoading } from "./loading";
import { showStatus } from "./status";

let current_page: number = 0; // the current page

async function gallery(page: number = current_page): Promise<void> {
  showLoading();
  try {
    const pictures_per_page: number = 10; // 2 pictures per page
    const max_num_of_pages: number = 5; // max number of pages to show in the pagination bar

    $(".pageNumber").off("click"); // remove all the clicks before starting anew

    current_page = page; // the current page
    const response = await fetch(
      `/api/GetPhotos?page=${current_page}&n=${pictures_per_page}&lang=${window.lang}`
    ); // replace with your API endpoint
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

    let inicial: number = 0;
    let final: number = num_of_pages;

    if (num_of_pages > max_num_of_pages) {
      if (current_page > 0) {
        $("#pages").append(`<button class="pageNumber" id="-1" ><</button>`);
      }

      final = current_page + Math.ceil(max_num_of_pages / 2);
      if (final > num_of_pages) {
        final = num_of_pages;
      }
      inicial = final - max_num_of_pages;
      if (inicial < 0) {
        inicial = 0;
        final = max_num_of_pages;
      }
    }

    for (let i = inicial; i < final; i++) {
      if (i == current_page) {
        $("#pages").append(
          `<button class="pageNumber selected" id="${i}" >${i + 1}</button>`
        );
      } else {
        $("#pages").append(
          `<button class="pageNumber" id="${i}" >${i + 1}</button>`
        );
      }
    }

    if (num_of_pages > max_num_of_pages && current_page < num_of_pages - 1) {
      $("#pages").append(`<button class="pageNumber" id="10000" >></button>`);
    }

    // Add click event listeners to the page buttons
    $(".pageNumber").on("click", function () {
      let next_page: number = parseInt($(this).attr("id"));
      if (next_page === -1) {
        next_page = current_page - 1;
      } else if (next_page == 10000) {
        next_page = current_page + 1;
      }
      gallery(next_page);
    });

    let i: number = 0;
    $("#mygallery").html("");
    data.Pictures.forEach((element: any) => {
      i++;
      //add elements to mygallery
      $("#mygallery").append(
        `<div><a href="${element.Uri}" target="_blank"><img class="grid-item grid-item-${i}" src="${element.ThumbnailUri}" alt="${element.Description}" /></a><p>${element.Description}</p></div>`
      );
    });
  } catch (error) {
    console.error("Error:", error);
    showStatus(error, "error");
  } finally {
    hideLoading();
  }
}

$(async () => {
  await gallery();
});
