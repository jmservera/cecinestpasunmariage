import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";

// import Swiper JS
import Swiper from "swiper";
import { Manipulation, Navigation, Pagination, Autoplay } from "swiper/modules";

const pictures_per_page: number = 20;
const pictures_delay: number = 5000;

// bind esc key to close fullscreen
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    document
      .getElementsByClassName("fullscreen")[0]
      .classList.remove("fullscreen");
  }
});

document.getElementById("fullScreen").addEventListener("click", (event) => {
  document.getElementsByClassName("swiper")[0].classList.add("fullscreen");
});

// use https://swiperjs.com/swiper-api
const swiper = new Swiper(".swiper", {
  modules: [Manipulation, Navigation, Pagination, Autoplay],
  loop: false,
  autoplay: { delay: pictures_delay },
  navigation: {
    nextEl: ".swiper-button-next",
    prevEl: ".swiper-button-prev",
  },
});

const observer = new IntersectionObserver((entries) => {
  entries.forEach((entry) => {
    if (entry.isIntersecting) {
      entry.target.classList.add("zoom-in");
    } else {
      entry.target.classList.remove("zoom-in");
    }
  });
});

const videoObserver = new IntersectionObserver((entries) => {
  entries.forEach((entry) => {
    var video = entry.target as HTMLVideoElement;
    if (video) {
      if (entry.isIntersecting) {
        video.play();
      } else {
        video.pause();
      }
    }
  });
});

async function loadPics(page: string): Promise<any> {
  let data: any = {};
  showLoading();
  try {
    const response: Response = await fetch(`/api/GetPhotos${page}`); // replace with your API endpoint
    data = await response.json();
    const template = document.getElementById(
      "slide-template",
    ) as HTMLTemplateElement;
    data.Pictures.forEach((element: any) => {
      const slide = template.content.cloneNode(true) as DocumentFragment;
      const title = slide.querySelector("p") as HTMLParagraphElement;
      title.textContent = element.Description;

      if (element.Uri.includes(".mp4")) {
        const video = slide.querySelector("video") as HTMLVideoElement;
        video.style.display = "block";
        video.src = element.Uri;
        video.title = element.Description;
        videoObserver.observe(video);
      } else {
        const img = slide.querySelector("img") as HTMLImageElement;
        img.style.display = "block";
        img.src = element.Uri;
        img.title = element.Description;
        observer.observe(img);
      }
      swiper.appendSlide(slide.firstElementChild as HTMLElement);
    });
    swiper.update();
  } catch (error) {
    console.error("Error:", error);
    showStatus(error, "error");
  } finally {
    hideLoading();
  }
  return data;
}

async function carousel(): Promise<void> {
  showLoading();
  try {
    let data = { Next: null };
    data = await loadPics(`?page=0&n=${pictures_per_page}&lang=${window.lang}`);
    swiper.autoplay.start();

    swiper.on("reachEnd", async () => {
      if (data.Next) {
        swiper.autoplay.pause();
        data = await loadPics(data.Next);
        swiper.autoplay.resume();
      } else {
        swiper.autoplay.stop();
        setTimeout(async () => {
          //unobserve all images
          swiper.slides.forEach((slide: HTMLElement) => {
            const img = slide.querySelector("img");
            observer.unobserve(img);
            const video = slide.querySelector("video");
            videoObserver.unobserve(video);
          });
          swiper.removeAllSlides();
          data = await loadPics(
            `?page=0&n=${pictures_per_page}&lang=${window.lang}`,
          );
          swiper.autoplay.start();
        }, 1000);
      }
    });
  } catch (error) {
    console.error("Error:", error);
    showStatus(error, "error");
  } finally {
    hideLoading();
  }
}

function showStatus(message: string, className: string = "") {
  const statusElement = document.getElementById("uploadStatus");
  statusElement.textContent = message;
  statusElement.className = `status ${className}`; // Apply success styling
  statusElement.style.display = "block";
  setTimeout(() => {
    statusElement.textContent = "";
    statusElement.className = ""; // Clear the status styling
    statusElement.style.display = "none"; // Hide the status element
  }, 5000);
}

$(async () => {
  await carousel();
});
