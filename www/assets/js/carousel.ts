import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";

// import Swiper JS
import Swiper from 'swiper';
import {Manipulation, Navigation, Pagination, Autoplay} from 'swiper/modules';

// bind esc key to close fullscreen
document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape') {
    document.getElementsByClassName('fullscreen')[0].classList.remove('fullscreen');
  }
});

document.getElementById('fullScreen').addEventListener('click', (event) => {
  document.getElementsByClassName('swiper')[0].classList.add('fullscreen');
});

// use https://swiperjs.com/swiper-api
const swiper = new Swiper('.swiper', {modules:[Manipulation, Navigation, Pagination, Autoplay], loop: false, autoplay: {delay: 5000},
  navigation: {
    nextEl: '.swiper-button-next',
    prevEl: '.swiper-button-prev',
  }});
let lastIndex:number=-1;
function playSlide(){
  if(lastIndex!=-1){
    const video = swiper.slides[lastIndex].querySelector('video');
    if(video){
      video.pause();
    }
    lastIndex=-1;
  }
  swiper.slides[swiper.activeIndex].querySelectorAll('video').forEach((video:HTMLVideoElement)=>{
    video.play();
    lastIndex=swiper.activeIndex;
  });
}
swiper.on('activeIndexChange', playSlide);

let current_page: number = 1; // the current page


async function carousel(page: number = current_page): Promise<void> {
  showLoading();
  try {
    const pictures_per_page: number = 10; // 2 pictures per page
    const max_num_of_pages: number = 5; // max number of pages to show in the pagination bar

    current_page = page; // the current page
    const response = await fetch(`/api/GetPhotos?page=${current_page}&lang=${window.lang}`); // replace with your API endpoint
    const data = await response.json();

    const total_num_of_pictures: number = data.NumPictures; // total number of pictures
    $("#pages2").html("");
    $("#pages2").append(`<div style="width: 100%;"> Número total de fotos: ${total_num_of_pictures} </div>`);

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
        $("#pages").append(`<button class="pageNumber selected" id="${i}" >${i}</button>`);
      } else {
        $("#pages").append(`<button class="pageNumber" id="${i}" >${i}</button>`);
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
      carousel(next_page);
    });

    swiper.autoplay.stop();
    swiper.removeAllSlides();
    data.Pictures.forEach((element: any) => {
      const slide = document.createElement('div') as HTMLDivElement;
      slide.className = "swiper-slide imgbox";
      const title = document.createElement('p') as HTMLParagraphElement;
      title.className="center";
      title.textContent=element.Description;
      slide.appendChild(title);
      if(element.Uri.includes(".mp4")){
        const item = document.createElement('video') as HTMLVideoElement;
        item.className="center-fit";
        item.autoplay=false;
        item.src=element.Uri;
        item.title=element.Description;
        slide.appendChild(item);      }
      else{
        const item = document.createElement('img') as HTMLImageElement;
        item.className="center-fit";
        item.src=element.Uri;
        item.title=element.Description;
        item.alt=element.Description;
        slide.appendChild(item);
      }
      swiper.appendSlide(slide);
    });
    swiper.update();
    swiper.autoplay.start();
    playSlide();


  } catch (error) {
    console.error("Error:", error);
    showStatus(error, "error");
  } finally {
    hideLoading();
  }
}

function showStatus(message: string, className: string = '') {
  const statusElement = document.getElementById('uploadStatus');
  statusElement.textContent = message;
  statusElement.className = `status ${className}`; // Apply success styling
  statusElement.style.display = 'block';
  setTimeout(() => {
    statusElement.textContent = '';
    statusElement.className = ''; // Clear the status styling
    statusElement.style.display = 'none'; // Hide the status element
  }, 5000);
}

$(async () => {
  await carousel();
});
