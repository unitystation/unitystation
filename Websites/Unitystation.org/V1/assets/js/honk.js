

    $('#patron-button').hover(
        function() {
            $('#patron-button-white').attr("src", "assets/img/become_a_patron_button.png");
        },
        function() {
            $('#patron-button-white').attr("src", "assets/img/become_a_patron_button-white.png");
        }
    );

var audioIsPlaying = false;

function toggleAudio(){
    var audio = document.getElementById("audio");

    if(!audioIsPlaying){
        audio.play();
        $("#audio-button-icon").attr("class","fas fa-volume-up");
        audioIsPlaying = true;
    }
    else{
        audio.pause();
        audio.currentTime = 0;
        $("#audio-button-icon").attr("class","fas fa-volume-off");
        audioIsPlaying = false;
    }
}

// get element once for optimization
$elem = $("#honk");

// animate the honk every 15 seconds
$(document).ready(function(e) {
    animateHonk()
});

function animateHonk(){
    // width is the screen width plus 150.
    // The +150 will allow the clown to leave the screen
    var width = "+=" + ($(document).width() + 150);

    // get a random value for the top offset between
    // 25px (top) and 250px (bottom)
    var top = Math.floor(Math.random() * (250 - 25 + 1)) + 25;

    // apply top offset...
    $elem.css("top",top);

    // animate the clown across the screen with the JQuery animate() function.
    // the first parameter are the css properties to animateHonk
    // the second parameter is the time it takes in miliseconds for the animation to background
    // the third parameter sets the animation to linear so it doesn't have awkward easing
    // the fourth parameter is what to do once the animation is complete.
    $elem.animate({
        left: width
    }, 10000, "linear", function() {
        // resent the clown's x position
        $elem.css("left", "-100px");
        // re-run this function after 5 seconds
        setTimeout(function(){ animateHonk() }, 8000);
    });
}

var headerImage = $('.headerImage');
var embeddedVideo = $('#embeddedVideo');
var videoWrapper = $('.videoWrapper');
var playButton = $('.playButton');
var closePlayer = $('.closePlayer');
var closePlayerMob = $('.closePlayerMob');

function playVideo(){
    embeddedVideo.attr("src", "https://www.youtube.com/embed/YKVmXn-Gv0M?autoplay=1?"+Math.random());
    videoWrapper.css("display","inline");
    headerImage.css("background-image","none");
    headerImage.css("background-color","black");
    playButton.css("display","none");
    closePlayer.css("display","inline");
    closePlayerMob.css("display","inline");
}

function closeVideo(){
    embeddedVideo.attr("src", "");
    videoWrapper.css("display","none");
    headerImage.removeAttr("style");
    playButton.css("display","inline");
    closePlayer.css("display","none");
    closePlayerMob.css("display","none");
}
