# Palheta de Cores do Projeto DevOps

Esta documentação define o padrão de cores extraído do projeto DevOps para ser aplicado na versão Inside.

## Cores Principais (Variáveis CSS)

| Variável | Hex | Descrição |
| :--- | :--- | :--- |
| `--bg-dark` | `#0f172a` | Fundo principal da aplicação |
| `--card-bg` | `#1e293b` | Fundo de cards, painéis e barras laterais |
| `--primary` | `#38bdf8` | Cor primária (Cyan/Sky), usada em botões ativos e destaques |
| `--primary-hover` | `#0ea5e9` | Cor primária ao passar o mouse |
| `--secondary` | `#334155` | Cor secundária para botões e elementos menos proeminentes |
| `--accent` | `#818cf8` | Cor de destaque (Indigo), usada em gradientes e detalhes |
| `--text-main` | `#f1f5f9` | Cor principal do texto (quase branco) |
| `--text-muted` | `#94a3b8` | Cor de texto secundária/suave |
| `--border` | `#334155` | Cor de bordas e divisores |

## Cores de Status e Contextuais

| Contexto | Hex / Gradiente | Descrição |
| :--- | :--- | :--- |
| **Sucesso / Live Preview** | `#4ade80` | Verde brilhante para código ao vivo e status de sucesso |
| **Sincronizado** | `#22c55e` | Verde para status de sincronização completa |
| **Erro** | `#ef4444` | Vermelho para erros e alertas críticos |
| **Aviso / Local** | `#f59e0b` | Laranja para avisos ou alterações locais |
| **Toast Erro** | `linear-gradient(135deg, #7f1d1d, #991b1b)` | Gradiente de fundo para notificações de erro |
| **Toast Aviso** | `linear-gradient(135deg, #78350f, #92400e)` | Gradiente de fundo para notificações de aviso |

## Aplicação Recomendada

1. **Branding**: O uso de gradientes entre `--primary` e `--accent` cria o visual moderno característico do DevOps.
2. **Código**: Blocos de código e visualizações em tempo real devem usar `#4ade80` para máxima legibilidade e sensação de "sistema ativo".
3. **Profundidade**: Use `--bg-dark` para o fundo e `--card-bg` com bordas em `--border` para criar hierarquia visual.
