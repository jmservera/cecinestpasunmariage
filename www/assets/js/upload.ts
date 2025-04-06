import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";
import { showStatus } from "./status";

function bindFileInputClick(elementId: string, fileInputId: string): void {
  const triggerElement = document.getElementById(elementId);
  const fileInputElement = document.getElementById(fileInputId);

  if (triggerElement === null || fileInputElement === null) {
    console.log("Elements not found");
    return;
  }
  triggerElement.addEventListener("click", function (event) {
    event.preventDefault();
    fileInputElement.click(); // Trigger the file input click
  });
  triggerElement.addEventListener("keydown", function (event) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault(); // Prevent the default action to ensure it works as expected
      fileInputElement.click();
    }
  });
  fileInputElement.addEventListener("change", uploadFiles);
}

async function uploadFiles(event: Event): Promise<void> {
  event.preventDefault(); // Prevent the default form submission
  showStatus(getTranslation("uploading"));
  try {
    const files: FileList = (event.target as HTMLInputElement).files; // Get the files from the input

    //loop for multiple files with a foreach
    showLoading();
    try {
      for (const file of Array.from(files)) {
        if (file) {
          try {
            // Use fetch API to send the file to the server
            const response = await fetch(`/api/upload?name=${file.name}`, {
              method: "POST",
              headers: {
                "Content-Type": file.type, // Set the Content-Type header to the file's MIME type
              },
              body: file, // Send the file directly as the body of the request
            });
            if (response.ok) {
              const result: string = await response.text(); // or response.text() if the server responds with text
              console.log("Success:", result);
              showStatus(getTranslation("uploadSuccess"), "success");
            } else {
              throw new Error("Upload failed");
            }
          } catch (error) {
            console.error("Error:", error);
            showStatus(getTranslation("uploadError"), "error");
          }
        } else {
          showStatus(getTranslation("uploadInvalid"), "error");
        }
      }
    } finally {
      hideLoading();
      try {
        (event.target as HTMLInputElement).value = ""; // Clear the file input
      } catch (error) {
        console.error("Error clearing files:", error);
      }
    }
  } catch (error) {
    console.error("Error:", error);
    showStatus(error, "error");
  }
}

document.addEventListener("DOMContentLoaded", function () {
  bindFileInputClick("cameraUpload", "imageInput");
  bindFileInputClick("customPictureUpload", "imageUpload");
});
