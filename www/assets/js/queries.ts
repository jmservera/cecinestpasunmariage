const usr: string =
    "{\nid\norigin\nemail\nname\nsurname\npartnerName\npax\nchildren\nalergies\ncomments\ncreatedAt\nupdatedAt\n}";

export const getByIdGql = "query getById($id: ID!) {\nuser_by_pk(id: $id) " + usr + "\n}";
export const createGql = "mutation create($item: CreateUserInput!) {\ncreateUser(item: $item) " + usr + "\n}";
export const updateGql = "mutation update($id: ID!, $_partitionKeyValue: String!, $item: UpdateUserInput!) " + "{\nupdateUser(id: $id, _partitionKeyValue: $_partitionKeyValue, item: $item) " + usr + "\n}";
export const getAllUsersGql = "{\nusers {\nitems " + usr + "\n}\n}";
