    // call api

    async function gallery()
    {
        try {
            const response = await fetch('/api/GetPhotos?page=1'); // replace with your API endpoint
            const data = await response.json();
            
            var i = 0;
            data.forEach(element => {
                i++;
                //add elements to mygallery
                $("#mygallery").append(
                    '<div>' +
                        '<img class="grid-item grid-item-'+i+'" src="'+element+'" alt="Image description" />' +
                        '<p>Title</p>' +
                    '</div>' 
                );
                console.log(element);
            });
            
        } catch (error) {
            console.error('Error:', error);
        }
        
    }

    $(document).ready(async () => {
        await gallery();
    });

