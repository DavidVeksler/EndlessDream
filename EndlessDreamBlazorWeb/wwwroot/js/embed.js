window.addEventListener("load", () => {

    let hostUrl = "https://localhost:7079";

    (function (d, i, f, ur) {
        let pdiv = document.createElement(d);
        pdiv.id = i;
        pdiv.style = "position:fixed;z-index:99999999;right:10px;bottom:0;height:96px;width:100px;min-height:96px;min-width:100px";
        let ifr = document.createElement(f);
        ifr.src = `${ur}`;
        ifr.style = "width: 100%; height: 100%; border: 0";
        pdiv.appendChild(ifr);
        document.body.appendChild(pdiv)
    })("div", "embed-iframe-container", "iframe", `${hostUrl}/embed`);

    (function (e, ur, dvid, h, s, v, w, x, y) {
        window.addEventListener(e, function (er) {
            if (er.origin != ur) return;
            let pdiv = document.getElementById(dvid);
            switch (er.data)
            {
                case h:
                    pdiv.style.height = v; pdiv.style.width = w;
                    break;
                case s:
                    pdiv.style.height = x; pdiv.style.width = y
            }
        }, false)
    })("message", hostUrl, "embed-iframe-container", "hide", "show", "96px", "100px", "680px", "430px");
})