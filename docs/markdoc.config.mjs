import { defineMarkdocConfig, component } from "@astrojs/markdoc/config";
import starlightMarkdoc from "@astrojs/starlight-markdoc";

export default defineMarkdocConfig({
  extends: [starlightMarkdoc()],
  tags: {
    ref: {
      render: component("./src/components/DocLinks.astro"),
      attributes: {
        terraform: { type: String },
        azure: { type: String },
        repo: { type: String },
        inline: { type: Boolean },
      },
    },
  },
});
