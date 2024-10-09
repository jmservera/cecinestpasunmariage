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
    const pictures_per_page: number = 50; // 2 pictures per page
    current_page = page; // the current page
    const response = await fetch(`/api/GetPhotos?page=${current_page}&n=${pictures_per_page}&lang=${window.lang}`); // replace with your API endpoint
    const data = await response.json();

    const total_num_of_pictures: number = data.NumPictures; // total number of pictures
    const num_of_pages: number = Math.ceil(
      total_num_of_pictures / pictures_per_page
    );

    let inicial: number = 1;
    let final: number = num_of_pages;

    swiper.autoplay.stop();
    swiper.removeAllSlides();
    const template = document.getElementById('slide-template') as HTMLTemplateElement;
    data.Pictures.forEach((element: any) => {      
      const slide = template.content.cloneNode(true) as DocumentFragment;
      const title = slide.querySelector('p') as HTMLParagraphElement;
      title.textContent = element.Description;
      
      if (element.Uri.includes(".mp4")) {
        const video = slide.querySelector('video') as HTMLVideoElement;
        video.style.display = 'block';
        video.src = element.Uri;
        video.title = element.Description;
      } else {
        const img = slide.querySelector('img') as HTMLImageElement;
        img.style.display = 'block';
        img.src = element.Uri;
        img.title = element.Description;
        img.alt = element.Description;
      }
      
      swiper.appendSlide(slide.firstElementChild as HTMLElement);
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
