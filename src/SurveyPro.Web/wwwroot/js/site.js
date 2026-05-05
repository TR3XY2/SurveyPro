// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
  const shell = document.getElementById("notifications-shell");

  if (!shell || !window.signalR || !window.bootstrap) {
    return;
  }

  const feedUrl = shell.dataset.notificationsUrl;
  const menuItems = document.getElementById("notification-menu-items");
  const toastContainer = document.getElementById(
    "notification-toast-container",
  );
  const counter = document.getElementById("notification-count");

  if (!feedUrl || !menuItems || !toastContainer || !counter) {
    return;
  }

  const notificationHistory = [];
  const seenNotificationIds = new Set();
  const viewedNotificationIds = new Set();
  const STORAGE_KEY = "viewedNotificationIds";

  // Load viewed notifications from localStorage
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      JSON.parse(stored).forEach((id) => viewedNotificationIds.add(id));
    }
  } catch (e) {
    // Ignore localStorage errors
  }

  function saveViewedNotifications() {
    try {
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify(Array.from(viewedNotificationIds)),
      );
    } catch (e) {
      // Ignore localStorage errors
    }
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect()
    .build();

  // Mark notifications as viewed when dropdown is opened
  shell.addEventListener("click", () => {
    notificationHistory.forEach((notif) => viewedNotificationIds.add(notif.id));
    saveViewedNotifications();
    updateNotificationCounter();
  });

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function formatTimestamp(value) {
    try {
      return new Date(value).toLocaleString();
    } catch {
      return "";
    }
  }

  function updateNotificationCounter() {
    const unseenCount = notificationHistory.filter(
      (n) => !viewedNotificationIds.has(n.id),
    ).length;

    if (unseenCount === 0) {
      counter.classList.add("d-none");
    } else {
      counter.textContent = unseenCount.toString();
      counter.classList.remove("d-none");
    }
  }

  function renderMenu() {
    menuItems.innerHTML = "";

    if (notificationHistory.length === 0) {
      menuItems.innerHTML =
        '<div class="dropdown-item-text text-muted small py-3 px-3">No notifications yet.</div>';
      counter.classList.add("d-none");
      return;
    }

    notificationHistory.slice(0, 10).forEach((notification) => {
      const item = document.createElement("div");
      item.className = "dropdown-item-text border-bottom py-2 px-3";
      item.innerHTML = `
				<div class="fw-semibold">${escapeHtml(notification.title)}</div>
				<div class="small text-body-secondary">${escapeHtml(notification.message)}</div>
				<div class="small text-muted mt-1">${escapeHtml(formatTimestamp(notification.createdAt))}</div>
			`;
      menuItems.appendChild(item);
    });

    updateNotificationCounter();
  }

  function showToast(notification) {
    const toast = document.createElement("div");
    toast.className = "toast border-0 text-bg-dark mb-2";
    toast.setAttribute("role", "alert");
    toast.setAttribute("aria-live", "assertive");
    toast.setAttribute("aria-atomic", "true");
    toast.innerHTML = `
			<div class="toast-header text-bg-dark border-0">
				<strong class="me-auto">${escapeHtml(notification.title)}</strong>
				<small class="text-light-emphasis">${escapeHtml(formatTimestamp(notification.createdAt))}</small>
				<button type="button" class="btn-close btn-close-white ms-2 mb-1" data-bs-dismiss="toast" aria-label="Close"></button>
			</div>
			<div class="toast-body">${escapeHtml(notification.message)}</div>
		`;

    toastContainer.prepend(toast);
    const bootstrapToast = new bootstrap.Toast(toast, { delay: 7000 });

    toast.addEventListener("hidden.bs.toast", () => {
      toast.remove();
    });

    bootstrapToast.show();
  }

  function registerNotification(notification, isHistorical) {
    // Skip if we've already registered this notification
    if (seenNotificationIds.has(notification.id)) {
      return;
    }

    seenNotificationIds.add(notification.id);
    notificationHistory.unshift(notification);

    if (!isHistorical) {
      showToast(notification);
    }

    renderMenu();
    updateNotificationCounter();
  }

  async function loadRecentNotifications() {
    const response = await fetch(feedUrl, {
      headers: { Accept: "application/json" },
    });

    if (!response.ok) {
      return;
    }

    const notifications = await response.json();
    notifications
      .reverse()
      .forEach((notification) => registerNotification(notification, true));
  }

  connection.on("notificationReceived", (notification) => {
    registerNotification(notification, false);
  });

  connection
    .start()
    .then(loadRecentNotifications)
    .catch(() => {
      counter.classList.add("d-none");
    });
})();
