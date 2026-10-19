"use strict";

const $ = (selector) => document.querySelector(selector);
const money = (value) =>
  Number(value).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const esc = (value) =>
  String(value).replace(
    /[&<>"']/g,
    (char) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[
        char
      ],
  );
const labels = {
  Pix: "Pix",
  Dinheiro: "Dinheiro",
  CartaoDebito: "Cartão de débito",
  CartaoCredito: "Cartão de crédito",
};
const dateLabel = (value) =>
  new Date(value).toLocaleString("pt-BR", {
    timeZone: "America/Sao_Paulo",
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
const state = {
  catalog: [],
  sales: [],
  report: [],
  cart: new Map(),
  customer: "",
  payment: "Pix",
  discount: 0,
  query: "",
  category: "",
  today: "",
  route: "home",
  busy: false,
  from: "",
  to: "",
  reportPayment: "",
};

const paths = {
  home: '<path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z"/>',
  bag: '<path d="M5 7h14l1 14H4L5 7Z"/><path d="M9 8V6a3 3 0 0 1 6 0v2"/>',
  chart: '<path d="M4 3v18h17M9 16v-5m5 5V6m5 10V9"/>',
  music:
    '<path d="M9 18V5l12-2v13M9 8l12-2"/><ellipse cx="6" cy="18" rx="3" ry="3"/><ellipse cx="18" cy="16" rx="3" ry="3"/>',
  arrow: '<path d="M4 12h16m-6-6 6 6-6 6"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  search: '<circle cx="10" cy="10" r="6"/><path d="m15 15 5 5"/>',
  revenue:
    '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18m-5 5h2"/>',
  ticket:
    '<path d="M3 7V4h18v3a3 3 0 0 0 0 6v7H3v-7a3 3 0 0 0 0-6Z"/><path d="M14 4v3m0 3v3m0 3v4"/>',
  download: '<path d="M12 3v12m-5-5 5 5 5-5M4 16v5h16v-5"/>',
  check: '<path d="m5 12 4 4L20 5"/>',
  filter: '<path d="M3 5h18l-7 8v6l-4 2v-8L3 5Z"/>',
};
function icon(name) {
  return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name] || paths.music}</svg>`;
}

function art(type, large = false) {
  const guitar = `<path d="M91 17 81 12l-6 16 6 5Z" fill="#a37b4c"/><path d="m77 28-4-2-22 63 7 3Z" fill="#654e37"/><path d="M56 77c-18-13-37 3-30 18 4 9-12 17-5 30 6 13 25 21 37 11 8-6 5-20 13-25 13-9 8-26-6-29l-9-5Z" fill="${type === "bass" ? "#677a63" : "#d0a16c"}" stroke="#523e2e" stroke-width="2"/><path d="M53 89c-6-9-17-2-13 5 3 6-8 12-4 19 4 8 15 13 21 6 4-4 0-13 6-17 6-4 2-12-4-12Z" fill="#f4e6cd"/><rect x="35" y="109" width="14" height="6" rx="1" transform="rotate(21 35 109)" fill="#535447"/><path d="m79 25-30 88m32-88-30 89" stroke="#ddd0ae" stroke-width="1"/><circle cx="63" cy="116" r="2" fill="#75654d"/><circle cx="59" cy="123" r="2" fill="#75654d"/><path d="m77 14-5-2m3 8-5-2m15-1 5 2m-7 4 5 2" stroke="#8c9390" stroke-width="3"/>`;
  const acoustic = `<path d="m88 13-10-3-6 18 7 3Z" fill="#9c643c"/><path d="m73 27-24 62 7 3 25-63Z" fill="#553d2b"/><path d="M52 75c-14-10-29 2-27 15 2 9-13 15-9 31 4 18 30 28 44 15 12-11 4-22 13-30 10-10 3-26-9-26Z" fill="#d9a364" stroke="#9b6a36" stroke-width="2"/><path d="M49 80c-12-7-23 4-19 14 2 8-13 15-8 28 5 13 22 20 34 11 11-9 3-19 11-28 9-9 3-22-8-22Z" fill="none" stroke="#eac28d" stroke-width="2"/><circle cx="47" cy="103" r="10" fill="#493c30" stroke="#b7864e" stroke-width="3"/><path d="m33 123 17 6" stroke="#674629" stroke-width="5"/><path d="m75 23-32 102m35-101-31 102" stroke="#e7d4a8" stroke-width=".8"/>`;
  const keyboard = `<rect x="11" y="57" width="126" height="48" rx="5" fill="#3d4540"/><path d="M20 83h108v17H20Z" fill="#f8f5e8"/><path d="M29 83v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17m9-17v17" stroke="#a1a59a"/><path d="M28 83v10m9-10v10m18-10v10m9-10v10m9-10v10m18-10v10m9-10v10m18-10v10" stroke="#303932" stroke-width="5"/><rect x="58" y="64" width="25" height="9" rx="1" fill="#9cae96"/><circle cx="28" cy="69" r="5" fill="#222c24"/><path d="M94 68h25m-25 4h25" stroke="#86927f" stroke-width="2"/><path d="m34 107 63 31m17-31-63 31" stroke="#596254" stroke-width="3"/>`;
  const drums = `<path d="m30 41-9 88m9-5 9 7m-18-7-9 7m105-90 11 83m-9 0-9 7m18-7 9 7" stroke="#757d70" stroke-width="2"/><ellipse cx="28" cy="40" rx="25" ry="5" fill="#c1a66c"/><ellipse cx="117" cy="40" rx="25" ry="5" fill="#c1a66c"/><rect x="39" y="56" width="31" height="27" rx="6" fill="#98584e" stroke="#c5c6b7" stroke-width="3"/><rect x="78" y="56" width="31" height="27" rx="6" fill="#98584e" stroke="#c5c6b7" stroke-width="3"/><circle cx="74" cy="110" r="30" fill="#a96355" stroke="#c8c9bb" stroke-width="4"/><circle cx="74" cy="110" r="24" fill="#e5e2d5"/><circle cx="84" cy="119" r="6" fill="#4e554a"/><path d="m50 130-6 10m51-10 5 10m-28-88 1-10m-18 2 41-16m-37-1 37 19" stroke="#8d8b76" stroke-width="2"/>`;
  const cajon = `<path d="m42 38 55 5 17 21-58-4Z" fill="#bd8b55"/><path d="m42 38 14 22v78l-14-20Z" fill="#9d6e41"/><path d="m56 60 58 4v78l-58-4Z" fill="#d5ad79" stroke="#b58c5c" stroke-width="2"/><circle cx="84" cy="104" r="14" fill="#4d4535"/><path d="m62 69 45 3m-45 58 45 3" stroke="#b38b59"/><circle cx="62" cy="68" r="1.4" fill="#765b40"/><circle cx="107" cy="72" r="1.4" fill="#765b40"/>`;
  return `<svg viewBox="0 0 150 155" fill="none" aria-hidden="true" ${large ? 'class="large-art"' : ""}>${type === "keyboard" ? keyboard : type === "drums" ? drums : type === "cajon" ? cajon : type === "acoustic" ? acoustic : guitar}</svg>`;
}

function heroArt() {
  return `<svg viewBox="0 0 430 310" fill="none" aria-hidden="true"><circle cx="250" cy="145" r="125" stroke="#52604b"/><circle cx="250" cy="145" r="99" stroke="#52604b"/><circle cx="250" cy="145" r="73" stroke="#52604b"/><path d="M34 190c70-140 200 120 363-95M17 210c70-140 200 120 363-95M49 170c70-140 200 120 363-95" stroke="#64745a" stroke-width=".6"/><g transform="translate(111 -12) rotate(18 100 150) scale(1.75)">${art("acoustic").replace(/<svg[^>]*>|<\/svg>/g, "")}</g><path d="m346 51 4 10 10 4-10 4-4 10-4-10-10-4 10-4Z" fill="#c8d1b4"/><circle cx="110" cy="65" r="3" fill="#e5a17b"/></svg>`;
}

async function api(path, options) {
  const response = await fetch(path, options);
  if (!response.ok) {
    let error;
    try {
      error = await response.json();
    } catch {
      error = null;
    }
    throw new Error(
      error?.message ||
        "Não foi possível concluir. Confira os dados e tente novamente.",
    );
  }
  return response.json();
}
let toastTimer;
function toast(message, error = false) {
  clearTimeout(toastTimer);
  const node = $("#toast");
  node.textContent = message;
  node.className = `toast${error ? " error" : ""}`;
  node.hidden = false;
  toastTimer = setTimeout(() => (node.hidden = true), 6000);
}
function metric(title, value, caption, name) {
  return `<div class="metric"><div class="metric-top"><span>${title}</span><span class="metric-icon">${icon(name)}</span></div><strong>${value}</strong><small>${caption}</small></div>`;
}
function metrics(sales, scope) {
  const revenue = sales.reduce((sum, sale) => sum + sale.total, 0);
  const quantity = sales.reduce((sum, sale) => sum + sale.quantity, 0);
  return `<div class="metrics">${metric("Receita de vendas", money(revenue), scope, "revenue")}${metric("Vendas realizadas", sales.length.toString().padStart(2, "0"), scope, "bag")}${metric("Instrumentos vendidos", quantity.toString().padStart(2, "0"), scope, "music")}${metric("Ticket médio", money(sales.length ? revenue / sales.length : 0), "Valor médio por pedido", "ticket")}</div>`;
}
function empty(title, description, name = "bag") {
  return `<div class="empty-state"><div class="empty-symbol">${icon(name)}</div><b>${title}</b>${description}</div>`;
}
function dateKey(value) {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: "America/Sao_Paulo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date(value));
  const get = (type) => parts.find((x) => x.type === type).value;
  return `${get("year")}-${get("month")}-${get("day")}`;
}
function home() {
  const todaySales = state.sales.filter(
    (s) => dateKey(s.createdAt) === state.today,
  );
  return `<div class="page-heading"><div><h1>Bom som. Bons negócios.</h1><p>Bem-vindo à Sonora. Vamos fazer a música acontecer?</p></div><a class="btn" href="#sale">${icon("plus")}Nova venda</a></div>
    <section class="hero"><div class="hero-copy"><div class="eyebrow">SUA LOJA, EM HARMONIA</div><h2>O próximo som<br>começa <em>aqui.</em></h2><p>Instrumentos que inspiram. Uma gestão que acompanha.</p><a class="btn" href="#sale">Explorar instrumentos ${icon("arrow")}</a></div><div class="hero-art">${heroArt()}</div><span class="hero-tag">MAIS QUE INSTRUMENTOS. POSSIBILIDADES.</span></section>
    <div class="section-label"><h2>Hoje na Sonora</h2><span><span class="status-dot"></span> Atualizado a cada venda</span></div>${metrics(todaySales, "No dia de hoje")}
    <div class="dashboard-bottom"><section class="panel"><div class="panel-heading"><h2>Últimas vendas</h2><a href="#reports">Ver relatório ${icon("arrow")}</a></div>${
      state.sales.length
        ? `<table><thead><tr><th>Cliente</th><th>Pagamento</th><th>Total</th></tr></thead><tbody>${state.sales
            .slice(0, 4)
            .map(
              (s) =>
                `<tr><td><b>${esc(s.customer)}</b><br><small class="muted">${dateLabel(s.createdAt)}</small></td><td><span class="badge">${labels[s.payment]}</span></td><td><b>${money(s.total)}</b></td></tr>`,
            )
            .join("")}</tbody></table>`
        : empty(
            "Sua primeira venda merece um bis.",
            "Comece um pedido e acompanhe seus resultados aqui.",
          )
    }</section>
    <section class="panel"><div class="panel-heading"><h2>No nosso catálogo</h2><small>Feitos para inspirar</small></div>${state.catalog
      .slice(0, 3)
      .map(
        (p) =>
          `<a class="featured" href="#sale"><span class="featured-art ${p.color}">${art(p.symbol)}</span><span><b>${p.name}</b><small>${p.brand}</small></span><strong>${money(p.price)}</strong></a>`,
      )
      .join("")}</section></div>`;
}
function productCards() {
  const found = state.catalog.filter(
    (p) =>
      (!state.category || p.category === state.category) &&
      `${p.name} ${p.brand}`
        .toLocaleLowerCase("pt-BR")
        .includes(state.query.toLocaleLowerCase("pt-BR")),
  );
  const count = $("#product-count");
  if (count) count.textContent = `${found.length} instrumentos disponíveis`;
  return found.length
    ? found
        .map(
          (p) =>
            `<article class="product"><div class="product-visual ${p.color}" data-symbol="${p.symbol}"><span class="product-category">${p.category}</span>${art(p.symbol)}</div><div class="product-info"><h3>${p.name}</h3><p>${p.brand}</p><div class="product-bottom"><strong>${money(p.price)}</strong><button class="add-btn" data-add="${p.id}" aria-label="Adicionar ${p.name}">+</button></div></div></article>`,
        )
        .join("")
    : empty(
        "Nenhum instrumento encontrado.",
        "Tente outro nome, marca ou categoria.",
        "search",
      );
}
function totals() {
  let subtotal = 0;
  for (const [id, quantity] of state.cart)
    subtotal += state.catalog.find((p) => p.id === id).price * quantity;
  const discount =
    Math.round(
      ((subtotal * Number(state.discount)) / 100 + Number.EPSILON) * 100,
    ) / 100;
  return { subtotal, discount, total: subtotal - discount };
}
function order() {
  const t = totals(),
    quantity = [...state.cart.values()].reduce((a, b) => a + b, 0);
  return `<div class="order-head"><h2>Seu pedido</h2><span>${quantity} ${quantity === 1 ? "instrumento" : "instrumentos"}</span></div><form id="checkout-form" class="order-body"><div class="field"><label for="customer">Cliente <span>*</span></label><input id="customer" name="customer" placeholder="Nome do cliente" required minlength="2" maxlength="80" autocomplete="name" value="${esc(state.customer)}"></div><div class="field"><label for="payment">Forma de pagamento</label><select id="payment">${Object.entries(
    labels,
  )
    .map(
      ([key, label]) =>
        `<option value="${key}" ${state.payment === key ? "selected" : ""}>${label}</option>`,
    )
    .join("")}</select></div>
    ${
      state.cart.size
        ? `<div class="cart-items">${[...state.cart]
            .map(([id, qty]) => {
              const p = state.catalog.find((p) => p.id === id);
              return `<div class="cart-item"><div><b>${p.name}</b><small>${p.brand}</small><div class="quantity-controls"><button type="button" data-minus="${id}" aria-label="Diminuir ${p.name}">−</button><span>${qty}</span><button type="button" data-add="${id}" aria-label="Aumentar ${p.name}">+</button></div></div><div class="cart-price">${money(p.price * qty)}<br><button class="remove-item" type="button" data-remove="${id}">Remover</button></div></div>`;
            })
            .join("")}</div>`
        : `<div class="cart-empty">${icon("bag")}<b>Uma nova história começa aqui.</b>Adicione instrumentos ao seu pedido.</div>`
    }
    <label class="discount-field" for="discount">Desconto no pedido (%)<input id="discount" type="number" min="0" max="30" step="0.01" value="${state.discount}" aria-label="Desconto percentual"></label><div class="order-totals"><div><span>Subtotal</span><span>${money(t.subtotal)}</span></div><div><span>Desconto</span><span>− ${money(t.discount)}</span></div><div class="grand-total"><span>Total</span><strong>${money(t.total)}</strong></div></div><button class="btn checkout" ${!state.cart.size || state.busy ? "disabled" : ""}>${icon("check")}${state.busy ? "Registrando…" : "Finalizar venda"}</button><p class="order-foot">Seu pedido será salvo com segurança neste dispositivo.</p>${state.cart.size ? '<button type="button" class="clear-cart" data-clear>Limpar pedido</button>' : ""}</form>`;
}
function sale() {
  return `<div class="page-heading"><div><h1>Encontre o próximo som.</h1><p>Escolha os instrumentos. A próxima história é por conta do cliente.</p></div><span class="eyebrow muted">NOVA VENDA</span></div><div class="sale-layout"><section><div class="toolbar"><div class="search">${icon("search")}<input id="search" type="search" aria-label="Buscar instrumento ou marca" placeholder="Buscar instrumento ou marca…" value="${esc(state.query)}"></div><select id="category" aria-label="Categoria"><option value="">Todas as categorias</option>${["Cordas", "Teclas", "Percussão"].map((c) => `<option ${state.category === c ? "selected" : ""}>${c}</option>`).join("")}</select></div><div class="catalog-meta"><span id="product-count">${state.catalog.length} instrumentos disponíveis</span><span>Curadoria Sonora</span></div><div class="product-grid" id="products">${productCards()}</div></section><aside class="panel order" aria-label="Pedido atual">${order()}</aside></div>`;
}
function reportQuery() {
  const params = new URLSearchParams();
  if (state.from) params.set("from", state.from);
  if (state.to) params.set("to", state.to);
  if (state.reportPayment) params.set("payment", state.reportPayment);
  return params.toString();
}
function reports() {
  return `<div class="page-heading"><div><h1>Cada venda, uma nova história.</h1><p>Acompanhe os resultados e encontre o ritmo do seu negócio.</p></div><button class="btn secondary" id="export">${icon("download")}Exportar CSV</button></div><form id="filter-form" class="report-toolbar"><div class="field"><label for="from">Data inicial</label><input id="from" type="date" value="${state.from}" required></div><div class="field"><label for="to">Data final</label><input id="to" type="date" value="${state.to}" required></div><div class="field"><label for="report-payment">Pagamento</label><select id="report-payment"><option value="">Todos os pagamentos</option>${Object.entries(
    labels,
  )
    .map(
      ([key, label]) =>
        `<option value="${key}" ${state.reportPayment === key ? "selected" : ""}>${label}</option>`,
    )
    .join(
      "",
    )}</select></div><button class="btn dark">${icon("filter")}Aplicar filtros</button></form><div class="section-label"><h2>Resultados do período</h2><span>Horário de Brasília</span></div>${metrics(state.report, "No período selecionado")}<section class="panel"><div class="panel-heading"><h2>Histórico de vendas</h2><small>${state.report.length} ${state.report.length === 1 ? "pedido encontrado" : "pedidos encontrados"}</small></div>${state.report.length ? `<div class="report-table"><table><thead><tr><th>Pedido / data</th><th>Cliente</th><th>Pagamento</th><th>Itens</th><th>Total</th><th></th></tr></thead><tbody>${state.report.map((s) => `<tr><td><b>#${s.id.slice(4, 12).toUpperCase()}</b><br><small class="muted">${dateLabel(s.createdAt)}</small></td><td>${esc(s.customer)}</td><td><span class="badge">${labels[s.payment]}</span></td><td>${s.quantity}</td><td><b>${money(s.total)}</b></td><td><button class="detail-button" data-details="${s.id}">Ver detalhes →</button></td></tr>`).join("")}</tbody></table></div>` : empty("Nenhuma venda por aqui, ainda.", "Ajuste os filtros ou registre uma nova venda.", "chart")}<div class="report-count">Os valores consideram os descontos aplicados em cada pedido.</div></section>`;
}
let routeVersion = 0;
async function navigate() {
  const version = ++routeVersion;
  const route = location.hash.slice(1);
  state.route = ["sale", "reports"].includes(route) ? route : "home";
  document
    .querySelectorAll("[data-nav]")
    .forEach((link) =>
      link.classList.toggle("active", link.dataset.nav === state.route),
    );
  $("#breadcrumb").textContent = {
    home: "Visão geral",
    sale: "Nova venda",
    reports: "Relatório de vendas",
  }[state.route];
  try {
    const sales = await api("/api/sales");
    if (version !== routeVersion) return;
    state.sales = sales;
    if (state.route === "reports") {
      const filtered = await api(`/api/sales?${reportQuery()}`);
      if (version !== routeVersion) return;
      state.report = filtered;
    }
    $("#main").innerHTML =
      state.route === "sale"
        ? sale()
        : state.route === "reports"
          ? reports()
          : home();
    if (state.route === "sale") productCards();
  } catch (error) {
    $("#main").innerHTML =
      `<div class="error-message">${esc(error.message)}</div><button class="btn secondary" id="retry">Tentar novamente</button>`;
  }
}
function refreshOrder() {
  $(".order").innerHTML = order();
}
function showDetails(sale, completed = false) {
  $("#dialog-content").innerHTML =
    `<div class="eyebrow muted">${completed ? "TUDO EM HARMONIA" : "DETALHES DO PEDIDO"}</div><h2>${completed ? "Venda registrada!" : "Uma nova história."}</h2><p>#${sale.id.slice(4, 12).toUpperCase()} · ${dateLabel(sale.createdAt)}<br>Cliente: ${esc(sale.customer)} · ${labels[sale.payment]}</p><table><thead><tr><th>Instrumento</th><th>Qtd.</th><th>Valor</th></tr></thead><tbody>${sale.items.map((item) => `<tr><td>${item.name}<br><small class="muted">${item.brand}</small></td><td>${item.quantity}</td><td>${money(item.total)}</td></tr>`).join("")}</tbody></table><div class="receipt-total"><span>Subtotal</span><span>${money(sale.subtotal)}</span></div><div class="receipt-total"><span>Desconto (${sale.discountPercent}%)</span><span>− ${money(sale.discount)}</span></div><div class="receipt-total last"><span>Total</span><span>${money(sale.total)}</span></div>`;
  $("#details").showModal();
}

document.addEventListener("input", (event) => {
  const node = event.target;
  if (node.id === "customer") state.customer = node.value;
  if (node.id === "search") {
    state.query = node.value.trim();
    $("#products").innerHTML = productCards();
  }
});
document.addEventListener("change", (event) => {
  const node = event.target;
  if (node.id === "payment") state.payment = node.value;
  if (node.id === "discount") {
    if (!node.checkValidity()) {
      node.reportValidity();
      return;
    }
    state.discount = Number(node.value);
    refreshOrder();
  }
  if (node.id === "category") {
    state.category = node.value;
    $("#products").innerHTML = productCards();
  }
});
document.addEventListener("click", async (event) => {
  const button = event.target.closest("button");
  if (!button) return;
  if (button.dataset.add) {
    const id = button.dataset.add,
      quantity = state.cart.get(id) || 0;
    if (quantity === 100) {
      toast("Limite de 100 unidades por instrumento.", true);
      return;
    }
    state.cart.set(id, quantity + 1);
    refreshOrder();
  }
  if (button.dataset.minus) {
    const id = button.dataset.minus,
      quantity = state.cart.get(id) - 1;
    if (!quantity) state.cart.delete(id);
    else state.cart.set(id, quantity);
    refreshOrder();
  }
  if (button.dataset.remove) {
    state.cart.delete(button.dataset.remove);
    refreshOrder();
  }
  if (
    button.hasAttribute("data-clear") &&
    confirm("Limpar todos os instrumentos deste pedido?")
  ) {
    state.cart.clear();
    refreshOrder();
  }
  if (button.dataset.details) {
    const sale = state.report.find((s) => s.id === button.dataset.details);
    if (sale) showDetails(sale);
  }
  if (button.id === "retry") navigate();
  if (button.id === "export") {
    const response = await fetch(`/api/report.csv?${reportQuery()}`);
    if (!response.ok) {
      toast("Não foi possível exportar o relatório.", true);
      return;
    }
    const url = URL.createObjectURL(await response.blob());
    const a = document.createElement("a");
    a.href = url;
    a.download = `sonora-vendas-${state.today}.csv`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    toast("Relatório exportado.");
  }
});
document.addEventListener("submit", async (event) => {
  if (event.target.id === "filter-form") {
    event.preventDefault();
    if ($("#from").value > $("#to").value) {
      toast("A data inicial deve ser anterior ou igual à data final.", true);
      return;
    }
    state.from = $("#from").value;
    state.to = $("#to").value;
    state.reportPayment = $("#report-payment").value;
    await navigate();
  }
  if (event.target.id !== "checkout-form") return;
  event.preventDefault();
  if (state.busy || !state.cart.size) return;
  state.customer = $("#customer").value;
  state.payment = $("#payment").value;
  state.discount = Number($("#discount").value);
  state.busy = true;
  refreshOrder();
  try {
    const saved = await api("/api/sales", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Sonora-Client": "preview",
      },
      body: JSON.stringify({
        customer: state.customer,
        payment: state.payment,
        discountPercent: state.discount,
        items: [...state.cart].map(([instrumentId, quantity]) => ({
          instrumentId,
          quantity,
        })),
      }),
    });
    state.cart.clear();
    state.customer = "";
    state.discount = 0;
    state.payment = "Pix";
    state.sales.unshift(saved);
    showDetails(saved, true);
  } catch (error) {
    toast(error.message, true);
  } finally {
    state.busy = false;
    refreshOrder();
  }
});
window.addEventListener("hashchange", navigate);

async function init() {
  document
    .querySelectorAll("[data-icon]")
    .forEach((node) => (node.innerHTML = icon(node.dataset.icon)));
  try {
    const [catalog, clock] = await Promise.all([
      api("/api/catalog"),
      api("/api/clock"),
    ]);
    state.catalog = catalog;
    state.today = clock.today;
    state.from = clock.today.slice(0, 7) + "-01";
    state.to = clock.today;
    $("#today-label").textContent = new Date(
      clock.today + "T12:00:00-03:00",
    ).toLocaleDateString("pt-BR", {
      day: "numeric",
      month: "long",
      year: "numeric",
      timeZone: "America/Sao_Paulo",
    });
    await navigate();
  } catch (error) {
    $("#main").innerHTML =
      `<div class="error-message">${esc(error.message)} Recarregue a página para tentar novamente.</div>`;
  }
  setTimeout(() => {
    $("#splash").classList.add("hidden");
    $("#splash").setAttribute("aria-hidden", "true");
  }, 800);
}
init();
