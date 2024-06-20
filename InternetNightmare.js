use ('p5')

use('hydra').then(init => init())


s0.init({
  src:document.querySelector('.p5Canvas'),
  dynamic:true
})


toggleHydra = function() {
  const hydra = document.querySelector('.graphics')
  if( hydra.style.display === 'block' ) {
    hydra.style.display = 'none'
  }else{
    hydra.style.display = 'block'
  }
}

let constraints = {     
  video: {       
    mandatory: {         
      minWidth: 1280,         
      minHeight: 720       },       
    optional: [{ maxFrameRate: 10 }]     },     
  audio: true   };   
capture = createCapture(constraints, function(stream) {     console.log(stream);   });  


draw = function() {     
  image(capture, 0, 0);
  if (keyIsPressed) {
    circle(width/2, height/2, 100)
    toggleHydra();
    
  }
}

keyIsPressed = function() {
  textSize(100);
  text_x = width / 2;
  text_y = height / 2;
  while (text_x < width){
    image(capture, 0, 0);
    text(key, text_x, text_y);
    text_x++;
  }
}

src(s0).saturate(2).contrast(1.3).layer(src(o0).mask(shape(4,2).scale(0.5,0.7).scrollX(0.25)).scrollX(0.001)).modulate(o0,0.001).out(o0)
