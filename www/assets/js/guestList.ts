type User = {
  id: string;
  email: string;
  name: string;
  pax: number;
  createdAt?: string;
  updatedAt?: string;
};

async function list(): Promise<User[]> {
  const query: string = `
        {
          users {
            items {
              id
              email
              name
              pax
              createdAt
              updatedAt
            }
          }
        }`;

  const endpoint: string = "/data-api/graphql";
  const response: Response = await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query: query }),
  });
  const result = await response.json();
  return result.data.users.items;

  console.table(result.data.users.items);
}

(async () => {
  let userList: User[] = await list();

  const guestDiv = document.querySelector("#guestList");
  let guestNode = document.createElement("table");
  let thead = document.createElement("thead");
  let thid = document.createElement("th");
  thid.innerText = "id";
  thead.appendChild(thid);
  let thname = document.createElement("th");
  thname.innerText = "name";
  thead.appendChild(thname);
  let themail = document.createElement("th");
  themail.innerText = "email";
  thead.appendChild(themail);
  let thpax = document.createElement("th");
  thpax.innerText = "pax";
  thead.appendChild(thpax);
  guestNode.appendChild(thead);

  userList.forEach((guest) => {
    let row = document.createElement("tr");
    guestNode.appendChild(row);

    let idT = document.createElement("td");
    idT.innerText = guest.id;
    let emailT = document.createElement("td");
    emailT.innerText = guest.email;
    let nameT = document.createElement("td");
    nameT.innerText = guest.name;
    let paxT = document.createElement("td");
    paxT.innerText = guest.pax.toString();

    row.appendChild(idT);
    row.appendChild(emailT);
    row.appendChild(nameT);
    row.appendChild(paxT);
  });
  guestDiv?.appendChild(guestNode);
})();
