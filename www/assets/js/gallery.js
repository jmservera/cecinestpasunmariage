    // call api

    async function gallery()
    {
        try {
            const response = await fetch('../api/GetPhotos'); // replace with your API endpoint
            const data = await response.json();

            data.forEach(element => {
                //add elements to mygallery
                $("#mygallery").append(
                    '<a href="'+element+'"><img src="'+element+'" alt="Image description" /></a>'
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

