import { getAllUsersGql } from "./queries";
import { UserInput, runQueryAll } from "./userQueries";
import { hideLoading, showLoading } from "./loading";

(async () => {
  showLoading();
  try {
    const guestDiv = document.querySelector("#guestList");
    if (!guestDiv) return console.error("guestList not found");

    guestDiv.textContent = "Loading...";
    let userList: UserInput[] = await runQueryAll(getAllUsersGql);
    guestDiv.textContent = "";

    let guestNode: HTMLTableElement = document.createElement("table");
    let thead: HTMLHeadElement = document.createElement("thead");
    Object.keys(userList[0]).forEach((key) => {
      let th = document.createElement("th");
      th.innerText = key;
      thead.appendChild(th);
    });
    guestNode.appendChild(thead);

    userList.forEach((guest) => {
      let row: HTMLTableRowElement = document.createElement("tr");
      guestNode.appendChild(row);

      Object.keys(guest).forEach((key) => {
        let td: HTMLTableCellElement = document.createElement("td");
        td.innerText = guest[key];
        row.appendChild(td);
      });
    });
    guestDiv?.appendChild(guestNode);
  } finally {
    hideLoading();
  }
})();
