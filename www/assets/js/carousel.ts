import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";
import { toCanvas } from "qrcode";

// import Swiper JS
import Swiper from "swiper";
import { Manipulation, Navigation, Pagination, Autoplay } from "swiper/modules";

const pictures_per_page: number = 20;
const pictures_delay: number = 5000;

const wakelocks = (function wakelock() {
  if ("wakeLock" in navigator) {
    let wakeLock = null;

    // Function to request a wake lock
    const requestWakeLock = async () => {
      try {
        wakeLock = await navigator.wakeLock.request("screen");
        console.log("Screen Wake Lock is active");
      } catch (err) {
        console.error(`${err.name}, ${err.message}`);
      }
    };

    // Function to release the wake lock
    const releaseWakeLock = async () => {
      if (wakeLock !== null) {
        await wakeLock.release();
        wakeLock = null;
        console.log("Screen Wake Lock is released");
      }
    };
    return { requestWakeLock, releaseWakeLock };
  }
})();

function awake(keepAwake: boolean) {
  // Check if the Wake Lock API is supported
  if (wakelocks) {
    if (keepAwake) {
      wakelocks.requestWakeLock();
    } else {
      wakelocks.releaseWakeLock();
    }
  } else {
    console.log("Wake Lock API not supported, trying with animation frame.");
    if (keepAwake) {
      // Prevent the device from going to sleep
      const preventSleep = () => {
        window.requestAnimationFrame(preventSleep);
      };
      preventSleep();
    } else {
      // Allow the device to go to sleep
      window.cancelAnimationFrame(0);
    }
  }
}

document.documentElement.addEventListener("fullscreenchange", (event) => {
  if (document.fullscreenElement) {
    document.getElementsByClassName("swiper")[0].classList.add("fullscreen");
    awake(true);
  } else {
    document.getElementsByClassName("swiper")[0].classList.remove("fullscreen");
    awake(false);
  }
});

document.getElementById("fullScreen").addEventListener("click", (event) => {
  // set F11 fullscreen mode
  if (document.documentElement.requestFullscreen) {
    document.documentElement.requestFullscreen();
  }
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

async function loadPics(page: string): Promise<any> {
  let data: any = {};
  showLoading();
  try {
    const response: Response = await fetch(`/api/GetPhotos${page}`); // replace with your API endpoint
    data = await response.json();
    const template = document.getElementById(
      "slide-template"
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
      } else {
        const img = slide.querySelector("img") as HTMLImageElement;
        img.style.display = "block";
        img.src = element.Uri;
        img.title = element.Description;
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
  const swiperBackground = document.querySelector(
    ".swiper-background"
  ) as HTMLElement;
  try {
    let data = { Next: null };
    data = await loadPics(`?page=0&n=${pictures_per_page}&lang=${window.lang}`);
    swiper.autoplay.start();

    var previousSlide = null;
    swiper.on("slideChange", () => {
      if (previousSlide) {
        const previousVideo = previousSlide.querySelector("video");
        if (previousVideo) {
          previousVideo.pause();
        }
        const previousImg = previousSlide.querySelector("img");
        if (previousImg) {
          previousImg.classList.remove("zoom-in");
        }
      }

      const currentSlide = swiper.slides[swiper.activeIndex];
      const img = currentSlide.querySelector("img");
      const video = currentSlide.querySelector("video") as HTMLVideoElement;
      if (img) {
        swiperBackground.style.backgroundImage = `url(${img.src})`;
        img.style.transform = "scale(1)";
        img.classList.add("zoom-in");
      }
      if (video) {
        video.play();
      }
      previousSlide = currentSlide;
    });

    swiper.on("reachEnd", async () => {
      if (data.Next) {
        swiper.autoplay.pause();
        data = await loadPics(data.Next);
        swiper.autoplay.resume();
      } else {
        swiper.autoplay.stop();
        setTimeout(async () => {
          swiper.removeAllSlides();
          data = await loadPics(
            `?page=0&n=${pictures_per_page}&lang=${window.lang}`
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
  const canvas = document.getElementById("qrcode") as HTMLCanvasElement;
  const qrCodeUrl = new URL("../fotos", window.location.href).href;
  toCanvas(canvas, qrCodeUrl);
});
