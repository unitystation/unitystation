var canvasWidthInPixels = 200,
    canvasHeightInPixels = 200,
    pixelWidth = 16,
    pixelHeight = 16,
    cursorOffset = 0.5,
    cursorLineW = 2,
    pixelRenderColor = "#808080",
    pixelRawColor = "#002080",
    drawPosRaw = [],
    mark,
    swatches = [
"#000000",
"#FFFFFF",
"#FF0000",
"#00FF00",
"#0000FF",
"#FFFF00",
"#00FFFF",
"#FF00FF"];


Color = function (hexOrObject) {
    var obj;
    if (hexOrObject instanceof Object) {
        obj = hexOrObject;
    } else {
        obj = LinearColorInterpolator.convertHexToRgb(hexOrObject);
    }
    console.log(obj.r);
    this.r = parseInt(obj.r, 10);
    this.g = parseInt(obj.g, 10);
    this.b = parseInt(obj.b, 10);
};
Color.prototype.asRgbCss = function () {
    return "rgb(" + this.r + ", " + this.g + ", " + this.b + ")";
};

function rgbToHex(rgb) {
    var hex = Number(rgb).toString(16);
    if (hex.length < 2) {
        hex = "0" + hex;
    }
    return hex;
}

function logicalToColor(indexA, indexB, interp, paletteSize) {
    return new Color({
        r: Math.min(255,Math.floor((indexA + 0.5) / paletteSize * 256.0)),
        g: Math.min(255,Math.floor((indexB + 0.5) / paletteSize * 256.0)),
        b: Math.floor(interp * 255.0) });
}


Color.prototype.asHex = function () {
    return "#" + rgbToHex(this.r) + rgbToHex(this.g) + rgbToHex(this.b);
};

Color.prototype.toRendered = function (paletteSize) {
    // console.log(paletteSize);
    const result = this.toLogical(paletteSize)

    const indexA = result.indexA;
    const indexB = result.indexB;
    const interp = result.interp;

    const paletteColorA = new Color(swatches[indexA]);
    const paletteColorB = new Color(swatches[indexB]);

    //console.log("Palette color A: " + paletteColorA.asHex());
    //console.log("Palette color B: " + paletteColorB.asHex());
    //console.log("lerp: " + interp);

    return LinearColorInterpolator.findColorBetween(
        paletteColorA,
        paletteColorB,
        interp * 100.0);
};

Color.prototype.toLogical = function (paletteSize) {
    // console.log(paletteSize);
    var indexA = Math.floor(Math.min(this.r / 255.0 * paletteSize, paletteSize-1));
    var indexB = Math.floor(Math.min(this.g / 255.0 * paletteSize, paletteSize-1));
    // console.log("indexA: "+ indexA);
    // console.log("indexB: "+ indexB);
    var interp = this.b / 255.0;

    return {"indexA":indexA, "indexB":indexB, "interp": interp}
}

var LinearColorInterpolator = {
    // convert 6-digit hex to rgb components;
    // accepts with or without hash ("335577" or "#335577")
    convertHexToRgb: function (hex) {
        match = hex.replace(/#/, "").match(/.{1,2}/g);
        return new Color({
            r: parseInt(match[0], 16),
            g: parseInt(match[1], 16),
            b: parseInt(match[2], 16) });

    },
    // left and right are colors that you're aiming to find
    // a color between. Percentage (0-100) indicates the ratio
    // of right to left. Higher percentage means more right,
    // lower means more left.
    findColorBetween: function (left, right, percentage) {
        newColor = {};
        components = ["r", "g", "b"];
        for (var i = 0; i < components.length; i++) {
            c = components[i];
            newColor[c] = Math.round(
                left[c] + (right[c] - left[c]) * percentage / 100);

        }
        return new Color(newColor);
    } };


// ==============================================

document.addEventListener("DOMContentLoaded", function () {
    var mouse = {};
    var oldTime, delta;
    var renderCanvas = document.getElementById("rendered");
    var rawOutputCanvas = document.getElementById("raw_output");
    var previewCanvas = document.getElementById("preview");
    var renderCtx = renderCanvas.getContext("2d");
    var rawOutputCtx = rawOutputCanvas.getContext("2d");
    var previewCtx = previewCanvas.getContext("2d");
    var globalPaletteSize = 8;

    function onPaletteChange() {
        const paletteSize = globalPaletteSize;

        for (const [i, value] of swatches.entries()) {
            swatches[i] = document.getElementById("palette" + i + "_color").value;
            //console.log("palette " + i + ": " + swatches[i]);
        }

        var indexA = parseInt(
            document.querySelector('input[name="PaletteA"]:checked').value);
        var indexB = parseInt(document.querySelector('input[name="PaletteB"]:checked').value);

        //console.log("Palette index A: " + indexA);
        //console.log("Palette index B: " + indexB);

        var interp = document.getElementById("mixRange").value / 255.0;
        //console.log("interp: " + interp);
        //console.log("interp2: " + rgbToHex(interp));

        var rawColor = logicalToColor(indexA, indexB, interp, paletteSize);

        //console.log(rawColor);
        pixelRawColor = rawColor.asHex();

        var renderColor = rawColor.toRendered(paletteSize);
        pixelRenderColor = renderColor.asHex();

        document.getElementById("mixed_color_rendered").value = pixelRenderColor;
        document.getElementById("mixed_color_raw").value = pixelRawColor;

        updateRenderColors(paletteSize);

        //console.log("RenderColor: " + pixelRenderColor);
        //console.log("RawColor: " + pixelRawColor);
    }


    renderCanvas.width = canvasWidthInPixels * pixelHeight;
    renderCanvas.height = canvasHeightInPixels * pixelHeight;
    previewCanvas.width = renderCanvas.width / pixelWidth;
    previewCanvas.height = renderCanvas.height / pixelHeight;
    rawOutputCanvas.width = previewCanvas.width;
    rawOutputCanvas.height = previewCanvas.height;

    for (const [i, value] of swatches.entries()) {
        document.getElementById("palette" + i + "_color").value = swatches[i];
    }

    onPaletteChange();

    function drawGrid() {
        renderCtx.beginPath();
        renderCtx.strokeStyle = "rgba(150, 150, 150, 0.75)";
        var x = 0,
            y = 0;
        while (x <= renderCanvas.width) {
            renderCtx.moveTo(x, 0);
            renderCtx.lineTo(x, renderCanvas.height);
            x += pixelWidth;
        }
        while (y <= renderCanvas.height) {
            renderCtx.moveTo(0, y);
            renderCtx.lineTo(renderCanvas.width, y);
            y += pixelHeight;
        }
        renderCtx.stroke();
    }

    function updateRenderColors(paletteSize)
    {
        for (var p = 0; p < drawPosRaw.length; p++) {
            drawPosRaw[p].renderedColor = drawPosRaw[p].rawColorObject.toRendered(paletteSize).asHex();
        }
    }

    function getMousePos(event) {
        var rect = renderCanvas.getBoundingClientRect();
        return {
            x:
            Math.round((event.clientX - rect.left - pixelWidth / 2) / pixelWidth) *
            pixelWidth,
            y:
            Math.round((event.clientY - rect.top - pixelHeight / 2) / pixelHeight) *
            pixelHeight };

    }

    function clearCanvas() {
        renderCtx.clearRect(0, 0, renderCanvas.width, renderCanvas.height);
        previewCtx.clearRect(0, 0, previewCanvas.width, previewCanvas.height);
        rawOutputCtx.clearRect(0, 0, rawOutputCanvas.width, rawOutputCanvas.height);
    }

    function createSwatchDiv(index)
    {
        const newDiv = $("<div class='swatch'>");
        const i = index;
        newDiv.append(`<label for='palette${i}'>Palette ${i}</label><input id='palette${i}_color' type='color' />`);
        newDiv.append(`<label for='paletteA_${i}'>A</label><input class='a-radio' type='radio' name='PaletteA' id='paletteA_${i}' value='${i}' />`);
        newDiv.append(`<label for='paletteB_${i}'>B</label><input class='b-radio' type='radio' name='PaletteB' id='paletteB_${i}' value='${i}' />`);
        newDiv.append(`<button class="delete-swatch">x</button>`);
        return newDiv;
    }

    function appendSwatch()
    {
        if (globalPaletteSize >= 255) return;
        var oldGlobalPaletteSize = globalPaletteSize++;

        for (var p=0; p < drawPosRaw.length; p++)
        {
            const logicalBefore = drawPosRaw[p].rawColorObject.toLogical(oldGlobalPaletteSize);
            const indexA = logicalBefore.indexA;
            const indexB = logicalBefore.indexB;
            const interp = logicalBefore.interp;
            const newColor = logicalToColor(indexA, indexB, interp, globalPaletteSize);
            drawPosRaw[p].rawColorObject = newColor;
            drawPosRaw[p].rawColor = newColor.asHex();
        }

        onPaletteChange();

        const newDiv = createSwatchDiv(oldGlobalPaletteSize);
        $("#swatch-collection").append(newDiv);

        swatches.push("#000000");
    }
    $("#add-swatch").click(appendSwatch);

    function drawImage() {
        var p = 0;
        while (p < drawPosRaw.length) {
            renderCtx.fillStyle = drawPosRaw[p].renderedColor || pixelRenderColor;
            previewCtx.fillStyle = renderCtx.fillStyle;

            rawOutputCtx.fillStyle = drawPosRaw[p].rawColor;

            renderCtx.fillRect(
                drawPosRaw[p].x,
                drawPosRaw[p].y,
                pixelWidth,
                pixelHeight);


            previewCtx.fillRect(
                drawPosRaw[p].x / pixelWidth,
                drawPosRaw[p].y / pixelHeight,
                1,
                1);


            rawOutputCtx.fillRect(
                drawPosRaw[p].x / pixelWidth,
                drawPosRaw[p].y / pixelHeight,
                1,
                1);

            p++;
        }
    }

    function drawMouse() {
        renderCtx.fillStyle = "rgba(255, 255, 255, 0.5)";
        renderCtx.fillRect(mouse.x, mouse.y, pixelWidth, cursorLineW);
        renderCtx.fillRect(mouse.x, mouse.y, cursorLineW, pixelHeight);

        renderCtx.fillStyle = pixelRenderColor;
        renderCtx.fillRect(
            mouse.x + cursorLineW,
            mouse.y + cursorLineW,
            pixelWidth * cursorOffset - 1,
            pixelHeight * cursorOffset - 1);

    }

    function render() {
        clearCanvas();
        drawGrid();
        drawImage();
        drawMouse();
        window.requestAnimationFrame(render);
    }

    function deleteSwatch() {
        if (globalPaletteSize <= 0) return;
        const deletedIndex = $(this).parent().index();
        var oldGlobalPaletteSize = globalPaletteSize--;

        const aSelectedIndex = $(".swatch:has(.a-radio:checked)").index();
        const bSelectedIndex = $(".swatch:has(.b-radio:checked)").index();

        $(".swatch").each(function(index, swatchDiv) {
            if (index >= deletedIndex) {
                const nextSwatch = $(swatchDiv).next();
                if (nextSwatch.length > 0 ) {
                    const nextColor = nextSwatch.children("input[type='color']").val();
                    const newDiv = createSwatchDiv(index);

                    newDiv.children("input[type='color']").val(nextColor);
                    newDiv.insertBefore(swatchDiv);
                }
                swatchDiv.remove();
            }
        });

        var wantedAIndex = Math.min(globalPaletteSize-1,aSelectedIndex);
        var wantedBIndex = Math.min(globalPaletteSize-1,bSelectedIndex);
        if (wantedAIndex > deletedIndex) {
            wantedAIndex--;
        }
        if (bSelectedIndex > deletedIndex) {
            wantedBIndex--;
        }

        console.log(wantedAIndex);
        console.log(wantedBIndex);
        console.log($(".swatch>.a-radio").eq(wantedAIndex));
        $(".swatch>.a-radio").eq(wantedAIndex).prop('checked', true);
        $(".swatch>.b-radio").eq(wantedBIndex).prop('checked', true);

        swatches.splice(deletedIndex,1);

        for (var p=drawPosRaw.length-1; p>=0; p--)
        {
            const logicalBefore = drawPosRaw[p].rawColorObject.toLogical(oldGlobalPaletteSize);
            var indexA = logicalBefore.indexA;
            var indexB = logicalBefore.indexB;

            if (indexA == deletedIndex || indexB == deletedIndex) {
                drawPosRaw.splice(p,1);
            } else {
                indexA = Math.min(globalPaletteSize-1, indexA);
                indexB = Math.min(globalPaletteSize-1, indexB);
                if (indexA > deletedIndex) { indexA--;}
                if (indexB > deletedIndex) { indexB--;}
                const interp = logicalBefore.interp;
                const newColor = logicalToColor(indexA, indexB, interp, globalPaletteSize);
                drawPosRaw[p].rawColorObject = newColor;
                drawPosRaw[p].rawColor = newColor.asHex();
            }
        }

        onPaletteChange();
    }

    window.requestAnimationFrame(render);
    $("#swatch-collection").on("change", "input[type='radio'],input[type='color']", onPaletteChange);
    $("#swatch-collection").on("click", ".delete-swatch", deleteSwatch);

    document.getElementById("mixRange").addEventListener("input", onPaletteChange);

    renderCanvas.addEventListener("mousemove", recordMouseMovement);
    renderCanvas.addEventListener("mousedown", startDrawing);
    renderCanvas.addEventListener("mouseup", stopDrawing);
    renderCanvas.addEventListener("contextmenu", clearPixel);
    document.getElementById("loadRaw").addEventListener("click", loadImage);

    function recordMouseMovement(event) {
        mouse = getMousePos(event);
    }

    function startDrawing(event) {
        if (event.button === 0) {
            mark = setInterval(function () {
                var pos = mouse;
                if (
                    drawPosRaw.length > 1 &&
                    drawPosRaw.slice(-1)[0].x == pos.x &&
                    drawPosRaw.slice(-1)[0].y == pos.y)
                {
                } else {
                    pos.rawColorObject = new Color(pixelRawColor);
                    pos.rawColor = pixelRawColor;
                    drawPosRaw.push(pos);
                }
            }, 10);
        }
    }

    function stopDrawing(event) {
        clearInterval(mark);
    }

    function clearPixel(event) {
        event.preventDefault();
        var savedPos = drawPosRaw.filter(function (savedPos) {
            return !(savedPos.x == mouse.x && savedPos.y == mouse.y);
        });
        drawPosRaw = savedPos;
        return false;
    }

    function loadImage()
    {
        drawPosRaw = [];
        image = new MarvinImage();
        var fileName = document.getElementById("file_to_load").files[0];
        if (fileName === undefined) return;
        var reader = new FileReader();
        reader.onload = function (event) {

            image.load(event.target.result, function () {
                var loadWidth = image.getWidth();
                var loadHeight = image.getHeight();
                console.log("loading image: (" + loadWidth + "x" + loadHeight + ")");
                for (var y = 0; y < loadHeight && y < canvasHeightInPixels; y++) {
                    for (var x = 0; x < loadWidth && x < canvasWidthInPixels; x++) {
                        var red = image.getIntComponent0(x, y);
                        var green = image.getIntComponent1(x, y);
                        var blue = image.getIntComponent2(x, y);
                        var alpha = image.getAlphaComponent(x, y);

                        if (alpha === 0) continue;

                        var col = new Color({ r: red, g: green, b: blue });
                        var pos = {
                            x: x * pixelWidth,
                            y: y * pixelHeight,
                            rawColor: col.asHex(),
                            rawColorObject: col };


                        drawPosRaw.push(pos);

                    }
                }
                updateRenderColors(globalPaletteSize);
            });
        };
        reader.readAsDataURL(fileName);
    }
});

function exportImage() {
    var p = 0;
    while (p < drawPosRendered.length) {
        drawPosRaw[p].width = pixelWidth;
        drawPosRaw[p].height = pixelHeight;
        p++;
    }
    var resp = "var img = { layers: " + JSON.stringify(drawPosRaw) + "}";
    document.getElementById("data").innerHTML = resp;
}

