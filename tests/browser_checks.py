"""Integração na interface de apoio. Inicie o Preview com um arquivo de vendas vazio e temporário.
Requer Python, Playwright e Chromium; veja README. As vendas deste teste são fictícias.
"""
import json
import os
from pathlib import Path
from playwright.sync_api import sync_playwright, expect

base = os.environ.get("SONORA_TEST_URL", "http://127.0.0.1:5080")
output = Path(__file__).resolve().parents[1] / "artifacts" / "screenshots"
output.mkdir(parents=True, exist_ok=True)

with sync_playwright() as p:
    browser = p.chromium.launch(executable_path=os.environ.get("CHROMIUM_PATH", "/usr/bin/chromium"), headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 1050}, device_scale_factor=1)
    errors = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.goto(base)
    expect(page.locator("#splash")).to_be_visible()
    page.screenshot(path=str(output / "01-abertura.png"))
    expect(page.locator("#splash")).to_have_class("splash hidden")
    expect(page.locator("#splash")).to_have_css("opacity", "0")
    expect(page.get_by_role("heading", name="Bom som. Bons negócios.")).to_be_visible()
    expect(page.get_by_text("Sua primeira venda merece um bis.")).to_be_visible()
    page.screenshot(path=str(output / "02-menu-vazio.png"), full_page=True)
    print("PASS abertura e menu sem dados fictícios")

    page.locator('[data-nav="sale"]').click()
    expect(page.locator(".product")).to_have_count(8)
    expect(page.get_by_role("button", name="Finalizar venda")).to_be_disabled()
    page.locator("#search").fill("Yamaha")
    expect(page.locator(".product")).to_have_count(2)
    page.locator("#category").select_option("Teclas")
    expect(page.locator(".product")).to_have_count(1)
    page.locator("#search").fill("nada-encontrado")
    expect(page.get_by_text("Nenhum instrumento encontrado.")).to_be_visible()
    page.locator("#search").fill("")
    page.locator("#category").select_option("")
    expect(page.locator("#product-count")).to_have_text("8 instrumentos disponíveis")
    page.screenshot(path=str(output / "03-catalogo.png"), full_page=True)
    print("PASS busca, categorias e estado vazio")

    page.get_by_role("button", name="Adicionar Guitarra Stratocaster", exact=True).click()
    page.get_by_role("button", name="Aumentar Guitarra Stratocaster", exact=True).click()
    page.get_by_role("button", name="Adicionar Violão Folk", exact=True).click()
    page.get_by_role("button", name="Diminuir Guitarra Stratocaster", exact=True).click()
    expect(page.locator(".order-head")).to_contain_text("2 instrumentos")
    page.locator("#customer").fill("Cliente demonstração")
    page.locator("#discount").fill("10")
    page.locator("#discount").press("Tab")
    expect(page.locator(".grand-total strong")).to_contain_text("2.150,10")
    page.screenshot(path=str(output / "04-venda.png"), full_page=True)
    page.get_by_role("button", name="Finalizar venda").click()
    expect(page.locator("#details")).to_be_visible()
    expect(page.get_by_role("heading", name="Venda registrada!")).to_be_visible()
    expect(page.locator(".receipt-total.last")).to_contain_text("2.150,10")
    page.screenshot(path=str(output / "05-comprovante.png"))
    page.get_by_role("button", name="Fechar detalhes").click()
    expect(page.get_by_role("button", name="Finalizar venda")).to_be_disabled()
    expect(page.locator("#customer")).to_have_value("")
    print("PASS carrinho, quantidade, desconto, finalização e comprovante")

    page.locator('[data-nav="reports"]').click()
    expect(page.locator("#main tbody tr")).to_have_count(1)
    expect(page.locator("#main tbody")).to_contain_text("Cliente demonstração")
    page.get_by_role("button", name="Ver detalhes").click()
    expect(page.locator("#details")).to_contain_text("Guitarra Stratocaster")
    expect(page.locator("#details")).to_contain_text("Violão Folk")
    page.get_by_role("button", name="Fechar detalhes").click()
    page.screenshot(path=str(output / "06-relatorio.png"), full_page=True)
    with page.expect_download() as download_event:
        page.get_by_role("button", name="Exportar CSV").click()
    csv = Path(download_event.value.path()).read_text(encoding="utf-8-sig")
    assert "Cliente demonstração" in csv and "2150,10" in csv
    page.locator("#report-payment").select_option("Dinheiro")
    page.get_by_role("button", name="Aplicar filtros").click()
    expect(page.get_by_text("Nenhuma venda por aqui, ainda.")).to_be_visible()
    page.locator("#report-payment").select_option("")
    page.get_by_role("button", name="Aplicar filtros").click()
    expect(page.locator("#main tbody tr")).to_have_count(1)
    print("PASS relatório, detalhes, filtros e arquivo CSV baixado")

    page.reload()
    expect(page.locator("#main tbody tr")).to_have_count(1)
    expect(page.locator("#splash")).to_have_css("opacity", "0")
    page.locator('[data-nav="home"]').click()
    expect(page.locator(".metric").first).to_contain_text("2.150,10")
    page.screenshot(path=str(output / "07-menu.png"), full_page=True)
    print("PASS recarga e indicadores atualizados")

    page.locator('[data-nav="sale"]').click()
    page.get_by_role("button", name="Adicionar Ukulele Concert", exact=True).click()
    page.locator('[data-nav="home"]').click()
    page.locator('[data-nav="sale"]').click()
    expect(page.locator(".cart-items")).to_contain_text("Ukulele Concert")
    page.once("dialog", lambda dialog: dialog.dismiss())
    page.get_by_role("button", name="Limpar pedido").click()
    expect(page.locator(".cart-items")).to_contain_text("Ukulele Concert")
    page.once("dialog", lambda dialog: dialog.accept())
    page.get_by_role("button", name="Limpar pedido").click()
    expect(page.get_by_role("button", name="Finalizar venda")).to_be_disabled()
    print("PASS navegação preserva pedido e limpeza exige confirmação")

    response = page.request.post(base + "/api/sales", headers={"X-Sonora-Client":"preview"}, data={"customer":"Teste inválido", "payment":"Pix", "discountPercent":31, "items":[{"instrumentId":"cajon", "quantity":1}]})
    assert response.status == 400
    response = page.request.post(base + "/api/sales", data={"customer":"Teste", "payment":"Pix", "discountPercent":0, "items":[]})
    assert response.status == 403
    assert len(page.request.get(base + "/api/sales").json()) == 1
    print("PASS API recusa pedido inválido e escrita sem cabeçalho da interface")

    page.set_viewport_size({"width":390,"height":844})
    page.locator('[data-nav="sale"]').click()
    expect(page.locator("#checkout-form")).to_be_visible()
    assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth"), "Overflow horizontal no celular"
    page.screenshot(path=str(output / "08-mobile.png"), full_page=True)
    assert not errors, json.dumps(errors)
    print("PASS layout móvel e ausência de erros JavaScript")
    browser.close()
print("Todos os fluxos de navegador foram aprovados.")
