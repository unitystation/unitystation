

jQuery(document).ready(function() {
// get element once for optimization
jQueryelem = jQuery("#honk");

// animate the honk every 15 seconds
jQuery(document).ready(function(e) {
    animateHonk()
});

function animateHonk(){
    // width is the screen width plus 150.
    // The +150 will allow the clown to leave the screen
    var width = "+=" + (jQuery(document).width() + 150);

    // get a random value for the top offset between
    // 25px (top) and 250px (bottom)
    var top = Math.floor(Math.random() * (250 - 25 + 1)) + 25;

    // apply top offset...
    jQueryelem.css("top",top);

    // animate the clown across the screen with the JQuery animate() function.
    // the first parameter are the css properties to animateHonk
    // the second parameter is the time it takes in miliseconds for the animation to background
    // the third parameter sets the animation to linear so it doesn't have awkward easing
    // the fourth parameter is what to do once the animation is complete.
    jQueryelem.animate({
        left: width
    }, 10000, "linear", function() {
        // resent the clown's x position
        jQueryelem.css("left", "-100px");
        // re-run this function after 5 seconds
        setTimeout(function(){ animateHonk() }, 8000);
    });
}

var headerImage = jQuery('.headerImage');
var embeddedVideo = jQuery('#embeddedVideo');
var videoWrapper = jQuery('.videoWrapper');
var playButton = jQuery('.playButton');
var closePlayer = jQuery('.closePlayer');
var closePlayerMob = jQuery('.closePlayerMob');

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
});