import { getAllUsersGql } from "./queries";
import { UserInput, runQueryAll } from "./userQueries";
import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";

(async () => {
  showLoading();
  try {
    const guestTable: HTMLTableElement =
      document.querySelector("#guestList table");
    if (!guestTable) return console.error("guestList table not found");

    const thead: HTMLHeadElement = guestTable.querySelector("thead");
    const tbody: HTMLTableSectionElement = guestTable.querySelector("tbody");

    let userList: UserInput[] = await runQueryAll(getAllUsersGql);

    const hiddenColumns = ["id", "origin", "createdAt", "updatedAt"];

    Object.keys(userList[0]).forEach((key) => {
      const th: HTMLTableCellElement = document.createElement("th");
      th.innerText = key;
      if (hiddenColumns.includes(key)) {
        th.hidden = true;
      }
      thead.appendChild(th);
    });
    // Add checkbox header
    const checkboxHeader: HTMLTableCellElement = document.createElement("th");
    const selectAllCheckbox: HTMLInputElement = document.createElement("input");
    selectAllCheckbox.type = "checkbox";
    selectAllCheckbox.title = getTranslation("selectAll");
    selectAllCheckbox.placeholder = "Select";
    checkboxHeader.appendChild(selectAllCheckbox);
    thead.appendChild(checkboxHeader);

    let checkboxes: HTMLInputElement[] = [];

    userList.forEach((guest) => {
      let row: HTMLTableRowElement = document.createElement("tr");
      tbody.appendChild(row);

      let firstCol: boolean = true;
      let toolTipCol: HTMLTableCellElement = null;
      let toolTipText: string = "";
      Object.keys(guest).forEach((key) => {
        let td: HTMLTableCellElement = document.createElement("td");
        td.innerText = guest[key];
        if (hiddenColumns.includes(key)) {
          td.hidden = true;
          toolTipText += key + ": " + guest[key] + "\n";
        } else if (firstCol) {
          toolTipText += key + ": " + guest[key] + "\n";
          toolTipCol = td;
          firstCol = false;
        } else {
          td.title = guest[key];
        }
        row.appendChild(td);
      });
      toolTipCol.title = toolTipText;

      // Add checkbox cell
      const checkboxCell: HTMLTableCellElement = document.createElement("td");
      const checkbox: HTMLInputElement = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.title = getTranslation("selectUser");
      checkbox.placeholder = "Select";
      checkbox.id = guest.email;
      checkbox.classList.add("guest-checkbox");
      checkboxCell.appendChild(checkbox);
      checkboxes.push(checkbox);
      row.appendChild(checkboxCell);
    });

    selectAllCheckbox.addEventListener("change", () => {
      checkboxes.forEach((checkbox) => {
        checkbox.checked = selectAllCheckbox.checked;
      });
    });

    checkboxes.forEach((checkbox) => {
      checkbox.addEventListener("change", () => {
        if (!checkbox.checked) {
          selectAllCheckbox.checked = false;
        } else if (Array.from(checkboxes).every((cb) => cb.checked)) {
          selectAllCheckbox.checked = true;
        }
      });
    });

    const sendMessageButton: HTMLButtonElement =
      document.querySelector("#sendMessage");
    const messageForm: HTMLFormElement = document.querySelector("#messageForm");

    sendMessageButton.addEventListener("click", async (e) => {
      e.preventDefault();
      const selectedGuests: string[] = [];
      tbody
        .querySelectorAll("input[type='checkbox']:checked")
        .forEach((checkbox) => {
          selectedGuests.push(checkbox.id);
        });

      if (selectedGuests.length === 0) {
        return alert("No guests selected");
      }

      const formData = new FormData(messageForm);
      const message = formData.get("message");
      const title = formData.get("title");

      if (!message) {
        return alert("Message cannot be empty");
      }

      try {
        await fetch("/api/SendEmail", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            recipients: selectedGuests,
            title: title,
            message: message,
          }),
        });
        alert("Emails sent successfully");
      } catch (error) {
        console.error("Error sending emails:", error);
        alert("Failed to send emails");
      }
    });
  } finally {
    hideLoading();
  }
})();
