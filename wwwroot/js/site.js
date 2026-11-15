(() => {
    // Visada naudojame Horizontai šviesią (balta/juoda/raudona) temą
    document.documentElement.removeAttribute("data-theme");
    localStorage.removeItem("vbe_theme");

    // Šviesdėžė (Lightbox)
    let currentZoom = 1.0;

    function openLightbox(src, title, width, height) {
        const dialog = document.getElementById("vbeLightbox");
        const img = document.getElementById("vbeLightboxImg");
        const caption = document.getElementById("vbeLightboxCaption");
        const dims = document.getElementById("vbeLightboxDims");
        if (!dialog || !img) return;

        currentZoom = 1.0;
        img.src = src;
        img.style.transform = "scale(1)";
        if (caption) caption.textContent = title || "Užduoties peržiūra";
        if (dims) dims.textContent = width && height ? `${width} × ${height} px` : "";

        if (typeof dialog.showModal === "function") {
            dialog.showModal();
        } else {
            dialog.setAttribute("open", "open");
        }
    }

    function zoomLightbox(delta, reset) {
        const img = document.getElementById("vbeLightboxImg");
        const label = document.getElementById("vbeLightboxZoomLabel");
        if (!img) return;

        currentZoom = reset
            ? 1.0
            : Math.min(3.0, Math.max(0.5, Math.round((currentZoom + delta) * 100) / 100));

        img.style.transform = `scale(${currentZoom})`;
        if (label) label.textContent = `${Math.round(currentZoom * 100)}%`;
    }

    function closeLightbox() {
        const dialog = document.getElementById("vbeLightbox");
        if (dialog && typeof dialog.close === "function") {
            dialog.close();
        }
    }

    document.addEventListener("click", (ev) => {
        const img = ev.target.closest(".vbe-img");
        if (img) {
            openLightbox(
                img.getAttribute("src"),
                img.getAttribute("alt") || "Užduotis",
                img.getAttribute("width"),
                img.getAttribute("height")
            );
            return;
        }

        const dialog = document.getElementById("vbeLightbox");
        if (dialog && ev.target === dialog) {
            closeLightbox();
        }
    });

    // Užduoties būsenos perjungimas
    async function toggleExerciseStatus(exerciseId, targetStatus) {
        const card = document.querySelector(`.exercise-card[data-ex-id="${exerciseId}"]`);
        const currentStatus = card ? parseInt(card.dataset.status || "0", 10) : 0;
        const nextStatus = currentStatus === targetStatus ? 0 : targetStatus;

        return await updateExerciseProgress(exerciseId, { status: nextStatus });
    }

    async function updateExerciseProgress(exerciseId, payload) {
        try {
            const resp = await fetch("/Api/Progress", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ exerciseId, ...payload })
            });

            if (!resp.ok) {
                showToast("Nepavyko išsaugoti. Patikrinkite, ar esate prisijungę.", "danger");
                return null;
            }

            const data = await resp.json();
            const card = document.querySelector(`.exercise-card[data-ex-id="${exerciseId}"]`);
            if (card) {
                card.dataset.status = String(data.status);

                const statusPill = card.querySelector(".ex-status-pill");
                if (statusPill) {
                    if (data.status === 1) {
                        statusPill.textContent = "Išspręsta";
                        statusPill.className = "status-pill ex-status-pill status-pill-solved";
                    } else if (data.status === 3 || data.status === 2) {
                        statusPill.textContent = "Pakartoti";
                        statusPill.className = "status-pill ex-status-pill status-pill-review";
                    } else {
                        statusPill.textContent = "Nespręsta";
                        statusPill.className = "status-pill ex-status-pill";
                    }
                }

                const btnSolved = card.querySelector(".btn-mark-solved");
                const btnReview = card.querySelector(".btn-mark-review");
                if (btnSolved) {
                    btnSolved.className = data.status === 1
                        ? "btn btn-sm btn-dark btn-mark-solved"
                        : "btn btn-sm btn-outline-secondary btn-mark-solved";
                    btnSolved.textContent = data.status === 1 ? "✓ Išspręsta" : "Pažymėti išspręsta";
                }
                if (btnReview) {
                    const isRev = data.status === 3 || data.status === 2;
                    btnReview.className = isRev
                        ? "btn btn-sm btn-primary btn-mark-review"
                        : "btn btn-sm btn-outline-secondary btn-mark-review";
                    btnReview.textContent = isRev ? "Atidėta pakartoti" : "Pakartoti vėliau";
                }
            }

            return data;
        } catch {
            showToast("Ryšio klaida saugant pakeitimus.", "danger");
            return null;
        }
    }

    async function saveExerciseNote(exerciseId) {
        const card = document.querySelector(`.exercise-card[data-ex-id="${exerciseId}"]`);
        const currentStatus = card ? parseInt(card.dataset.status || "0", 10) : 0;
        const input = document.getElementById(`note-input-${exerciseId}`);
        if (!input) return;

        const res = await updateExerciseProgress(exerciseId, {
            status: currentStatus,
            personalNote: input.value
        });
        if (res) {
            showToast("Pastaba išsaugota", "dark");
        }
    }

    // Sprendimo lenta (Scratchpad Canvas)
    const scratchpads = new Map();

    function initScratchpad(exId) {
        if (scratchpads.has(exId)) return;
        const canvas = document.getElementById(`scratch-canvas-${exId}`);
        if (!canvas) return;

        const rect = canvas.getBoundingClientRect();
        canvas.width = Math.max(560, Math.floor(rect.width || 720));
        canvas.height = 280;

        const ctx = canvas.getContext("2d");
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.lineWidth = 2.2;
        ctx.strokeStyle = "#141414";

        let drawing = false;
        const state = { canvas, ctx, color: "#141414", width: 2.2 };

        function getPos(e) {
            const r = canvas.getBoundingClientRect();
            const scaleX = canvas.width / r.width;
            const scaleY = canvas.height / r.height;
            const clientX = e.touches ? e.touches[0].clientX : e.clientX;
            const clientY = e.touches ? e.touches[0].clientY : e.clientY;
            return { x: (clientX - r.left) * scaleX, y: (clientY - r.top) * scaleY };
        }

        function start(e) {
            drawing = true;
            const p = getPos(e);
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
            if (e.cancelable) e.preventDefault();
        }

        function move(e) {
            if (!drawing) return;
            const p = getPos(e);
            ctx.strokeStyle = state.color;
            ctx.lineWidth = state.width;
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            if (e.cancelable) e.preventDefault();
        }

        function stop() {
            drawing = false;
        }

        canvas.addEventListener("mousedown", start);
        canvas.addEventListener("mousemove", move);
        window.addEventListener("mouseup", stop);
        canvas.addEventListener("touchstart", start, { passive: false });
        canvas.addEventListener("touchmove", move, { passive: false });
        canvas.addEventListener("touchend", stop);

        scratchpads.set(exId, state);
    }

    function toggleScratchpad(exId) {
        const wrap = document.getElementById(`scratch-wrap-${exId}`);
        if (!wrap) return;
        const isHidden = wrap.classList.contains("d-none");
        wrap.classList.toggle("d-none", !isHidden);
        if (isHidden) {
            setTimeout(() => initScratchpad(exId), 20);
        }
    }

    function setScratchColor(exId, color, width, btnEl) {
        initScratchpad(exId);
        const s = scratchpads.get(exId);
        if (s) {
            s.color = color;
            s.width = width || 2.2;
        }
        const wrap = document.getElementById(`scratch-wrap-${exId}`);
        if (wrap && btnEl) {
            wrap.querySelectorAll(".color-swatch").forEach(el => el.classList.remove("active"));
            if (btnEl.classList.contains("color-swatch")) {
                btnEl.classList.add("active");
            }
        }
    }

    function clearScratchpad(exId) {
        const s = scratchpads.get(exId);
        if (s) {
            s.ctx.clearRect(0, 0, s.canvas.width, s.canvas.height);
        }
    }

    function expandScratchpad(exId) {
        initScratchpad(exId);
        const s = scratchpads.get(exId);
        if (!s) return;

        const snapshot = document.createElement("canvas");
        snapshot.width = s.canvas.width;
        snapshot.height = s.canvas.height;
        snapshot.getContext("2d").drawImage(s.canvas, 0, 0);

        const nextHeight = s.canvas.height < 420 ? 480 : 280;
        s.canvas.height = nextHeight;
        s.canvas.style.height = `${nextHeight}px`;
        s.ctx.lineCap = "round";
        s.ctx.lineJoin = "round";
        s.ctx.drawImage(snapshot, 0, 0);
    }

    function toggleStageZoom(stageId) {
        const el = document.getElementById(stageId);
        if (el) el.classList.toggle("zoom-125");
    }

    function showToast(message, variant = "dark") {
        const container = document.getElementById("vbeToastContainer");
        if (!container) return;

        const toast = document.createElement("div");
        toast.className = `alert alert-${variant} shadow-sm py-2 px-3 mb-2 small`;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = "0";
            toast.style.transition = "opacity 0.2s ease";
            setTimeout(() => toast.remove(), 220);
        }, 2000);
    }

    window.VbeApp = {
        openLightbox,
        zoomLightbox,
        closeLightbox,
        toggleExerciseStatus,
        updateExerciseProgress,
        saveExerciseNote,
        toggleScratchpad,
        setScratchColor,
        clearScratchpad,
        expandScratchpad,
        toggleStageZoom,
        showToast
    };
})();
